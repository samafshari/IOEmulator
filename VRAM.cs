using System;

namespace Neat;

public class VramSurface
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    // Backing pixel storage (RGB per pixel)
    public RGB[] Buffer { get; private set; }

    public int Stride => Width; // pixels per row
    public int ByteLength => Width * Height * 3; // 3 bytes per pixel (RGB)

    public VramSurface(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentException("Invalid VRAM dimensions");
        Width = width;
        Height = height;
        Buffer = new RGB[width * height];
    }

    public VramSurface(RGB[] buffer, int width, int height)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (width <= 0 || height <= 0) throw new ArgumentException("Invalid VRAM dimensions");
        if (buffer.Length != width * height) throw new ArgumentException("Buffer length does not match dimensions");
        Width = width;
        Height = height;
        Buffer = buffer;
    }

    public int ToIndex(int x, int y) => y * Width + x;

    public void SetPixel(int x, int y, RGB color)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        Buffer[ToIndex(x, y)] = color;
    }

    public RGB GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) throw new ArgumentOutOfRangeException();
        return Buffer[ToIndex(x, y)];
    }

    // Read a linear slice of VRAM as raw bytes (RGBRGB...) starting at 'offset' for 'count' bytes
    public byte[] ReadBytes(int offset, int count)
    {
        if (offset < 0 || count < 0 || offset + count > ByteLength)
            throw new ArgumentOutOfRangeException();
        var result = new byte[count];
        int startPixel = offset / 3;
        int startChannel = offset % 3; // 0=R,1=G,2=B
        int remaining = count;
        int dst = 0;
        int pixelIndex = startPixel;
        int channel = startChannel;
        while (remaining > 0)
        {
            if ((uint)pixelIndex >= (uint)Buffer.Length) break;
            var c = Buffer[pixelIndex];
            byte val = channel == 0 ? c.R : channel == 1 ? c.G : c.B;
            result[dst++] = val;
            remaining--;
            channel++;
            if (channel == 3)
            {
                channel = 0;
                pixelIndex++;
            }
        }
        return result;
    }

    // Write bytes into VRAM (RGBRGB...) starting at 'offset'
    public void WriteBytes(int offset, ReadOnlySpan<byte> data)
    {
        if (offset < 0 || offset + data.Length > ByteLength)
            throw new ArgumentOutOfRangeException();
        int startPixel = offset / 3;
        int startChannel = offset % 3;
        int src = 0;
        int remaining = data.Length;
        int pixelIndex = startPixel;
        int channel = startChannel;
        RGB cur = Buffer[pixelIndex];
        while (remaining > 0)
        {
            byte b = data[src++];
            remaining--;
            if (channel == 0) cur.R = b;
            else if (channel == 1) cur.G = b;
            else cur.B = b;
            channel++;
            if (channel == 3)
            {
                Buffer[pixelIndex] = cur;
                pixelIndex++;
                if ((uint)pixelIndex >= (uint)Buffer.Length) break;
                cur = Buffer[pixelIndex];
                channel = 0;
            }
        }
        if (channel != 0 && (uint)pixelIndex < (uint)Buffer.Length)
        {
            Buffer[pixelIndex] = cur;
        }
    }
}
