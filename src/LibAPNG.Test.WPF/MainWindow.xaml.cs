using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LibAPNG.Test.WPF;

public partial class MainWindow : Window
{
    private APNGBitmap _apng;
    private Frame[] _frames;
    private BitmapSource[] _bitmapCache;
    private int _currentFrameIndex;
    private DispatcherTimer _timer;
    private bool _isPlaying;

    public MainWindow()
    {
        InitializeComponent();
        AllowDrop = true;
        Drop += MainWindow_Drop;
        DragOver += MainWindow_DragOver;

        _timer = new DispatcherTimer { IsEnabled = false };
        _timer.Tick += Timer_Tick;
    }

    // ── File loading ────────────────────────────────────────────────────────

    private void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select APNG File",
            Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
            LoadFile(dlg.FileName);
    }

    private void MainWindow_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void MainWindow_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0)
                LoadFile(files[0]);
        }
    }

    private void LoadFile(string path)
    {
        StopAnimation();

        try
        {
            TxtStatus.Text = $"Loading: {path}";

            _apng = new APNGBitmap(path);

            if (_apng.IsSimplePNG)
            {
                // Static PNG — just display it
                _frames = null;
                _bitmapCache = null;
                ImgDisplay.Source = LoadBitmapFromStream(_apng.DefaultImage.GetStream());
                TxtPlaceholder.Visibility = Visibility.Collapsed;
                CheckerBg.Visibility = Visibility.Visible;
                ProgressBar.Visibility = Visibility.Collapsed;
                BtnPlay.IsEnabled = false;
                BtnPause.IsEnabled = false;
                BtnStop.IsEnabled = false;
                TxtInfo.Text = $"Static PNG  |  {_apng.IHDRChunk.Width} × {_apng.IHDRChunk.Height}";
                TxtStatus.Text = $"Loaded static PNG: {Path.GetFileName(path)}";
                return;
            }

            _frames = _apng.Frames;
            _bitmapCache = new BitmapSource[_frames.Length];

            // Pre-decode all frames so playback is smooth
            for (int i = 0; i < _frames.Length; i++)
                _bitmapCache[i] = LoadBitmapFromStream(_frames[i].GetStream());

            _currentFrameIndex = 0;

            // Update UI
            TxtPlaceholder.Visibility = Visibility.Collapsed;
            CheckerBg.Visibility = Visibility.Visible;
            ProgressBar.Visibility = Visibility.Visible;

            FrameSlider.Maximum = _frames.Length - 1;
            FrameSlider.Value = 0;
            UpdateFrameDisplay(0);

            var plays = _apng.acTLChunk?.NumPlays ?? 0;
            TxtInfo.Text = $"APNG  |  {_apng.IHDRChunk.Width} × {_apng.IHDRChunk.Height}  |  {_frames.Length} 帧  |  循环: {(plays == 0 ? "∞" : plays.ToString())}";
            TxtStatus.Text = $"Loaded: {Path.GetFileName(path)}";

            BtnPlay.IsEnabled = true;
            BtnPause.IsEnabled = false;
            BtnStop.IsEnabled = false;

            // Auto-play
            StartAnimation();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "Failed to load";
        }
    }

    private static BitmapSource LoadBitmapFromStream(MemoryStream ms)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    // ── Playback ─────────────────────────────────────────────────────────────

    private void StartAnimation()
    {
        if (_frames == null || _frames.Length == 0) return;
        _isPlaying = true;
        SetTimerForFrame(_currentFrameIndex);
        _timer.Start();
        BtnPlay.IsEnabled = false;
        BtnPause.IsEnabled = true;
        BtnStop.IsEnabled = true;
        TxtStatus.Text = "Playing...";
    }

    private void PauseAnimation()
    {
        if (!_isPlaying) return;
        _isPlaying = false;
        _timer.Stop();
        BtnPlay.IsEnabled = true;
        BtnPause.IsEnabled = false;
        BtnStop.IsEnabled = true;
        TxtStatus.Text = "Paused";
    }

    private void StopAnimation()
    {
        _isPlaying = false;
        _timer.Stop();
        if (_frames != null)
        {
            _currentFrameIndex = 0;
            if (_bitmapCache != null)
                UpdateFrameDisplay(0);
        }
        if (_frames != null)
        {
            BtnPlay.IsEnabled = true;
            BtnPause.IsEnabled = false;
            BtnStop.IsEnabled = false;
        }
        TxtStatus.Text = _frames != null ? "Stopped" : "Ready";
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        _timer.Stop();
        _currentFrameIndex = (_currentFrameIndex + 1) % _frames.Length;
        UpdateFrameDisplay(_currentFrameIndex);
        SetTimerForFrame(_currentFrameIndex);
        _timer.Start();
    }

    private void SetTimerForFrame(int index)
    {
        var fcTL = _frames[index].fcTLChunk;
        double ms;
        if (fcTL != null)
        {
            ushort num = fcTL.DelayNum;
            ushort den = fcTL.DelayDen;
            // den == 0 is treated as 100 per APNG spec
            if (den == 0) den = 100;
            ms = num == 0 ? 10 : (num * 1000.0 / den);
        }
        else
        {
            ms = 100; // fallback 100ms
        }
        _timer.Interval = TimeSpan.FromMilliseconds(ms);
        TxtFrameTime.Text = $"{(int)ms} ms";
    }

    private void UpdateFrameDisplay(int index)
    {
        if (_bitmapCache == null || index >= _bitmapCache.Length) return;

        ImgDisplay.Source = _bitmapCache[index];

        FrameSlider.Value = index;

        var fcTL = _frames[index].fcTLChunk;
        int frameNum = fcTL != null ? (int)fcTL.SequenceNumber + 1 : index + 1;
        TxtFrameLabel.Text = $"Frame(s) {frameNum}/{_frames.Length}";
    }

    // ── Button handlers ──────────────────────────────────────────────────────

    private void BtnPlay_Click(object sender, RoutedEventArgs e) => StartAnimation();

    private void BtnPause_Click(object sender, RoutedEventArgs e) => PauseAnimation();

    private void BtnStop_Click(object sender, RoutedEventArgs e) => StopAnimation();

    // ── Slider ───────────────────────────────────────────────────────────────

    private void FrameSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_frames == null || _bitmapCache == null) return;
        int idx = (int)FrameSlider.Value;
        if (idx == _currentFrameIndex) return;

        bool wasPlaying = _isPlaying;
        if (wasPlaying) _timer.Stop();

        _currentFrameIndex = idx;
        UpdateFrameDisplay(idx);

        if (wasPlaying)
        {
            SetTimerForFrame(idx);
            _timer.Start();
        }
    }
}
