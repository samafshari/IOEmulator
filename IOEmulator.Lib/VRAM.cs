using System;

namespace Neat;

public class VramSurface
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    // Backing pixel storage: palette indices (one byte per pixel)
    public byte[] Buffer { get; private set; }

    public int Stride => Width; // pixels per row
    public int ByteLength => Width * Height; // 1 byte per pixel (index)

    public VramSurface(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentException("Invalid VRAM dimensions");
        Width = width;
        Height = height;
        Buffer = new byte[width * height];
    }

    public VramSurface(byte[] buffer, int width, int height)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (width <= 0 || height <= 0) throw new ArgumentException("Invalid VRAM dimensions");
        if (buffer.Length != width * height) throw new ArgumentException("Buffer length does not match dimensions");
        Width = width;
        Height = height;
        Buffer = buffer;
    }

    public int ToIndex(int x, int y) => y * Width + x;

    public void SetPixel(int x, int y, byte colorIndex)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        Buffer[ToIndex(x, y)] = colorIndex;
    }

    public byte GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) throw new ArgumentOutOfRangeException();
        return Buffer[ToIndex(x, y)];
    }

    // Read a linear slice of VRAM as raw bytes (indices) starting at 'offset' for 'count' bytes
    public byte[] ReadBytes(int offset, int count)
    {
        if (offset < 0 || count < 0 || offset + count > ByteLength)
            throw new ArgumentOutOfRangeException();
        var result = new byte[count];
        Array.Copy(Buffer, offset, result, 0, count);
        return result;
    }

    // Write bytes (indices) starting at 'offset'
#if NETSTANDARD2_0
    public void WriteBytes(int offset, byte[] data)
#else
    public void WriteBytes(int offset, ReadOnlySpan<byte> data)
#endif
    {
        if (offset < 0 || offset + data.Length > ByteLength)
            throw new ArgumentOutOfRangeException();
#if NETSTANDARD2_0
        Array.Copy(data, 0, Buffer, offset, data.Length);
#else
        data.CopyTo(Buffer.AsSpan(offset));
#endif
    }
}
