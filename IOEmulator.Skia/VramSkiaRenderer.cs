using System;
using SkiaSharp;

namespace Neat;

// Renders the IOEmulator's current frame (PixelBuffer32 packed int[] ARGB) onto an SKCanvas
public class VramSkiaRenderer : IDisposable
{
    private IOEmulator? _io;
    private SKBitmap? _bitmap; // RGBA8888 backing
    private byte[]? _rgba;

    // Rendering options
    public bool NearestNeighbor = true;

    public void Attach(IOEmulator io)
    {
        _io = io ?? throw new ArgumentNullException(nameof(io));
        EnsureBitmap();
    }

    private void EnsureBitmap()
    {
        if (_io == null) return;
        int w = _io.ResolutionW, h = _io.ResolutionH;
        if (w <= 0 || h <= 0) return;
        if (_bitmap == null || _bitmap.Width != w || _bitmap.Height != h)
        {
            _bitmap?.Dispose();
            _bitmap = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            _rgba = new byte[w * h * 4];
        }
    }

    // Convert packed ARGB int[] to RGBA byte array
    private void UpdateRgba()
    {
        if (_io == null || _rgba == null) return;
        var src = _io.PixelBuffer32;
        if (src == null) return;
        int len = src.Length;
        if (len * 4 != _rgba.Length)
        {
            // Resolution changed without reattach; rebuild
            EnsureBitmap();
            if (_io.PixelBuffer32 == null || _rgba == null) return;
            src = _io.PixelBuffer32;
            len = src.Length;
        }
        int di = 0;
        for (int i = 0; i < len; i++)
        {
            int c = src[i];
            _rgba[di++] = (byte)((c >> 16) & 0xFF); // R
            _rgba[di++] = (byte)((c >> 8) & 0xFF);  // G
            _rgba[di++] = (byte)(c & 0xFF);         // B
            _rgba[di++] = (byte)((c >> 24) & 0xFF); // A
        }
    }

    // Draw entire VRAM into dest rectangle on canvas
    public void Draw(SKCanvas canvas, SKRect dest)
    {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
    if (_io == null) return;
        EnsureBitmap();
        if (_bitmap == null || _rgba == null) return;

        UpdateRgba();
        // Copy bytes into SKBitmap
        var ptr = _bitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(_rgba, 0, ptr, _rgba.Length);

        using var paint = new SKPaint
        {
            FilterQuality = NearestNeighbor ? SKFilterQuality.None : SKFilterQuality.High,
            IsAntialias = !NearestNeighbor
        };
        var srcRect = new SKRect(0, 0, _bitmap.Width, _bitmap.Height);
        canvas.DrawBitmap(_bitmap, srcRect, dest, paint);
    }

    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
