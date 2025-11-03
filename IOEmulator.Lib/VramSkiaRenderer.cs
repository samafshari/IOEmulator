using System;
using SkiaSharp;

namespace Neat;

// Renders the IOEmulator's VRAM (RGB[]) onto an SKCanvas
public class VramSkiaRenderer : IDisposable
{
    private VramSurface? _vram;
    private SKBitmap? _bitmap; // RGBA8888 backing
    private byte[]? _rgba;

    // Rendering options
    public bool NearestNeighbor = true;

    public void Attach(IOEmulator io)
    {
        _vram = io?.VRAM ?? throw new ArgumentNullException(nameof(io));
        EnsureBitmap();
    }

    public void Attach(VramSurface vram)
    {
        _vram = vram ?? throw new ArgumentNullException(nameof(vram));
        EnsureBitmap();
    }

    private void EnsureBitmap()
    {
        if (_vram == null) return;
        int w = _vram.Width, h = _vram.Height;
        if (w <= 0 || h <= 0) return;
        if (_bitmap == null || _bitmap.Width != w || _bitmap.Height != h)
        {
            _bitmap?.Dispose();
            _bitmap = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            _rgba = new byte[w * h * 4];
        }
    }

    // Convert VRAM RGB to RGBA byte array
    private void UpdateRgba()
    {
        if (_vram == null || _rgba == null) return;
        var src = _vram.Buffer;
        int len = src.Length;
        int di = 0;
        for (int i = 0; i < len; i++)
        {
            var c = src[i];
            _rgba[di++] = c.R; // R
            _rgba[di++] = c.G; // G
            _rgba[di++] = c.B; // B
            _rgba[di++] = 255; // A
        }
    }

    // Draw entire VRAM into dest rectangle on canvas
    public void Draw(SKCanvas canvas, SKRect dest)
    {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (_vram == null) return;
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
