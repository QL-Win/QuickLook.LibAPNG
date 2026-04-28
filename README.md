# APNG.NET
*A fully-managed APNG Parser*

## Introduction
I've been searching for days looking for a simple, easy-to-use animation controller for my game engine until I found [this article](http://www.codeproject.com/Articles/36179/APNG-Viewer). Then I noticed I could use APNG to bundle multiply image into one single file and describe the animation process internally (no coding needed). In APNG format, each frame have an `fcTL` chunk (frame control chunk), which contains many useful information such as `frame_height`; `x_offset` and `delay_time`. So we can set all these up when we build an APNG and copy it directly to game folder - no any coding needed.

## PNG and APNG specification support status

*   For simple PNG, All chunks but `IHDR`, `IDAT` and `IEND` are **unsupported**, and will be **ignored** during the parsing.
*   For APNG, the library **can only** parse `IHDR`, `acTL`, `fcTL`, `IDAT`, `fdAT` and `IEND` chunks.
*   Multiply frame sizes are **supported**. This means you can reduce the file size by using *Differential Frames*. (use [APNG Anime Maker](https://sites.google.com/site/cphktool/apng-anime-maker))
*   All `DISPOSE_OP` and `BLEND_OP` are **supported**.

## About the code

Basic component:

*   **APNG Parser**
>   An managed DLL which handle parsing APNG image file.
>   This library is *backward-compatible*: It means you can use this library to read an simple PNG image, with no error produced.

## Useful links

*   [http://en.wikipedia.org/wiki/APNG](http://en.wikipedia.org/wiki/APNG)
*   [PNG (Portable Network Graphics) Specification](http://www.libpng.org/pub/png/spec/1.2/png-1.2-pdg.html)
*   [APNG (Animated Portable Network Graphics) Specification](https://wiki.mozilla.org/APNG_Specification)
*   [APNG Anime Maker](https://sites.google.com/site/cphktool/apng-anime-maker)
