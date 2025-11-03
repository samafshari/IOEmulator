using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Threading;

namespace Neat;

public struct RGB(byte r, byte g, byte b)
{
    public byte R = r;
    public byte G = g;
    public byte B = b;
}

public struct Coords
{
    public int X;
    public int Y;
}

public class Glyph(int width, byte[] bitmap)
{
    public int Width = width; // Height is implied by Bitmap length / Width
    public int Height => Bitmap == null ? 0 : (Bitmap.Length / Width);
    public byte[]? Bitmap = bitmap;
    public Action? Action;
}

public partial class CodePage(Glyph[] glyphs)
{
    public Glyph[] Glyphs = glyphs;
}

public struct ScreenMode(int textCols, int textRows, int resW, int resH, RGB[] palette, CodePage cp)
{
    public int TextCols = textCols;
    public int TextRows = textRows;
    public int ResolutionW = resW;
    public int ResolutionH = resH;
    public RGB[] Palette = palette;
    public CodePage CodePage = cp;
}

public class IOEmulator
{
    // Pixel VRAM surface wrapper
    public VramSurface VRAM { get; private set; } = new VramSurface(1, 1);
    public CodePage CodePage = CodePage.IBM8x8();
    public RGB[] Palette = IOPalettes.EGA;
    public int TextCols;
    public int TextRows;
    public int ResolutionW;
    public int ResolutionH;
    public RGB[] PixelBuffer = [];
    public int BackgroundColorIndex;
    public int ForegroundColorIndex;
    public int CursorX;
    public int CursorY;
    public int TurtleX;
    public int TurtleY;

    // ===== Input subsystem =====
    private readonly ConcurrentQueue<KeyEvent> _keyQueue = new();
    private readonly AutoResetEvent _keySignal = new(false);
    public event Action<KeyEvent>? KeyReceived;
    // Optional external provider (host) to poll keys if queue is empty
    public ReadKeyDelegate? ExternalReadKey;

    public IOEmulator()
    {
        LoadQBasicScreenMode(0);
    }

    public void LoadScreenMode(ScreenMode mode)
    {
        TextCols = mode.TextCols;
        TextRows = mode.TextRows;
        ResolutionW = mode.ResolutionW;
        ResolutionH = mode.ResolutionH;
        Palette = mode.Palette;
        CodePage = mode.CodePage;
    PixelBuffer = new RGB[ResolutionW * ResolutionH];
    VRAM = new VramSurface(PixelBuffer, ResolutionW, ResolutionH);
        ClearPixelBuffer();
        CursorX = 0;
        CursorY = 0;
        TurtleX = 0;
        TurtleY = 0;
        ResetView();
        ResetWindow();
    }

    public void LoadQBasicScreenMode(int modeIndex)
    {
        var mode = IOScreenModes.GetQBasicScreenMode(modeIndex);
        LoadScreenMode(mode);
    }

    public RGB GetColor(int index)
    {
        if (index < 0 || index >= Palette.Length)
            throw new ColorOutOfRangeException(index, Palette.Length);
        return Palette[index];
    }

    public void SetColor(int index, RGB color)
    {
        if (index < 0 || index >= Palette.Length)
            throw new ColorOutOfRangeException(index, Palette.Length);
        Palette[index] = color;
    }

    public RGB ReadPixelAt(int x, int y)
    {
        if (x < 0 || x >= ResolutionW || y < 0 || y >= ResolutionH)
            throw new IOEmulatorException("Pixel coordinates out of range.");
        return PixelBuffer[y * ResolutionW + x];
    }

    public RGB ReadPixelClipped(int x, int y)
    {
        if ((uint)x >= (uint)ResolutionW || (uint)y >= (uint)ResolutionH) return new RGB(0,0,0);
        if (IsClipped(x, y)) return new RGB(0,0,0);
        return PixelBuffer[y * ResolutionW + x];
    }

    public void WritePixelAt(int x, int y, RGB color)
    {
        if (x < 0 || x >= ResolutionW || y < 0 || y >= ResolutionH)
            throw new IOEmulatorException("Pixel coordinates out of range.");
        PixelBuffer[y * ResolutionW + x] = color;
    }

    public void WritePixelAt(int x, int y, int colorIndex)
    {
        if (x < 0 || x >= ResolutionW || y < 0 || y >= ResolutionH)
            throw new IOEmulatorException("Pixel coordinates out of range.");
        PixelBuffer[y * ResolutionW + x] = GetColor(colorIndex);
    }

    // Clipping state (VIEW)
    public int ClipX1 = 0, ClipY1 = 0, ClipX2 = int.MaxValue, ClipY2 = int.MaxValue; // inclusive

    public void ResetView()
    {
        ClipX1 = 0; ClipY1 = 0; ClipX2 = ResolutionW - 1; ClipY2 = ResolutionH - 1;
    }

    public void SetView(int x1, int y1, int x2, int y2)
    {
        if (x2 < x1 || y2 < y1) throw new IOEmulatorException("Invalid view rectangle.");
        ClipX1 = Math.Max(0, x1);
        ClipY1 = Math.Max(0, y1);
        ClipX2 = Math.Min(ResolutionW - 1, x2);
        ClipY2 = Math.Min(ResolutionH - 1, y2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsClipped(int x, int y)
        => x < ClipX1 || x > ClipX2 || y < ClipY1 || y > ClipY2;

    // Safe pixel write honoring clip and bounds
    public void WritePixelClipped(int x, int y, RGB color)
    {
        if ((uint)x >= (uint)ResolutionW || (uint)y >= (uint)ResolutionH) return;
        if (IsClipped(x, y)) return;
        PixelBuffer[y * ResolutionW + x] = color;
    }

    public void WriteTextAt(int col, int row, int charCode)
    {
        WriteTextAt(col, row, charCode, ForegroundColorIndex, BackgroundColorIndex);
    }

    public void WriteTextAt(int col, int row, int charCode, int fgColorIndex, int bgColorIndex)
    {
        if (col < 0 || col >= TextCols || row < 0 || row >= TextRows)
            throw new IOEmulatorException("Text coordinates out of range.");
        var bg = GetColor(bgColorIndex);
        var fg = GetColor(fgColorIndex);
        PutGlyphAtCell(charCode, col, row, bg, fg);
    }

    public void ClearPixelBuffer()
    {
        RGB bgColor = GetColor(BackgroundColorIndex);
        Array.Fill(PixelBuffer, bgColor);
    }

    // WINDOW (world-to-screen) mapping
    public bool WindowEnabled = false;
    public double WinX1 = 0, WinY1 = 0, WinX2 = 1, WinY2 = 1; // world coords

    public void SetWindow(double wx1, double wy1, double wx2, double wy2)
    {
        if (wx2 == wx1 || wy2 == wy1) throw new IOEmulatorException("Invalid window extents.");
        WinX1 = wx1; WinY1 = wy1; WinX2 = wx2; WinY2 = wy2; WindowEnabled = true;
    }

    public void ResetWindow()
    {
        WindowEnabled = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int sx, int sy) WorldToScreen(double x, double y)
    {
        if (!WindowEnabled) return ((int)Math.Round(x), (int)Math.Round(y));
        // Map world to viewport (current view / clip area)
        double vx1 = ClipX1, vy1 = ClipY1, vx2 = ClipX2, vy2 = ClipY2;
        double u = (x - WinX1) / (WinX2 - WinX1);
        double v = (y - WinY1) / (WinY2 - WinY1);
        int sx = (int)Math.Round(vx1 + u * (vx2 - vx1));
        int sy = (int)Math.Round(vy1 + v * (vy2 - vy1));
        return (sx, sy);
    }

    public void SetTextDimensions(int width, int height)
    {
        TextCols = width;
        TextRows = height;
        CursorX = 0;
        CursorY = 0;
    }

    public void SetPixelDimensions(int width, int height)
    {
        ResolutionW = width;
        ResolutionH = height;
        PixelBuffer = new RGB[ResolutionW * ResolutionH];
        VRAM = new VramSurface(PixelBuffer, ResolutionW, ResolutionH);
        ClearPixelBuffer();
    }

    public void Cls()
    {
        ClearPixelBuffer();
        CursorX = 0;
        CursorY = 0;
    }

    // ===== Input API =====
    public void InjectKey(in KeyEvent ev)
    {
        _keyQueue.Enqueue(ev);
        _keySignal.Set();
        KeyReceived?.Invoke(ev);
    }

    public bool TryReadKey(out KeyEvent ev)
    {
        if (_keyQueue.TryDequeue(out ev)) return true;
        if (ExternalReadKey != null)
        {
            var ext = ExternalReadKey();
            if (ext.HasValue)
            {
                ev = ext.Value;
                return true;
            }
        }
        ev = default;
        return false;
    }

    public KeyEvent WaitForKey(CancellationToken cancellationToken = default)
    {
        if (TryReadKey(out var evImmediate)) return evImmediate;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);
            // Wait until signaled or periodically poll external
            _keySignal.WaitOne(10);
            if (TryReadKey(out var ev)) return ev;
        }
    }

    public void LocateCursor(int col, int row)
    {
        if (col < 0 || col >= TextCols || row < 0 || row >= TextRows)
            throw new IOEmulatorException("Cursor coordinates out of range.");
        CursorX = col;
        CursorY = row;
    }

    public void PutChar(int charCode)
    {
        if (charCode == 7) // BEL - Bell
        {
            // Handle bell - for now, ignore
            return;
        }
        else if (charCode == 8) // BS - Backspace
        {
            if (CursorX > 0)
            {
                CursorX--;
            }
            return;
        }
        else if (charCode == 9) // TAB - Horizontal Tab (every 8 cols)
        {
            int nextTabStop = ((CursorX / 8) + 1) * 8;
            if (nextTabStop >= TextCols)
            {
                // move to new line
                CursorX = 0;
                CursorY++;
                if (CursorY >= TextRows)
                {
                    CursorY = TextRows - 1;
                    ScrollTextUp(1);
                }
            }
            else
            {
                CursorX = nextTabStop;
            }
            return;
        }
        else if (charCode == 13) // CR - Carriage Return
        {
            CursorX = 0;
            return;
        }
        else if (charCode == 10) // LF - Line Feed
        {
            CursorY++;
            if (CursorY >= TextRows)
            {
                CursorY = TextRows - 1;
                ScrollTextUp(1);
            }
            return;
        }
        // Else, write the character and advance cursor
        WriteTextAt(CursorX, CursorY, charCode);
        CursorX++;
        if (CursorX >= TextCols)
        {
            CursorX = 0;
            CursorY++;
            if (CursorY >= TextRows)
            {
                CursorY = TextRows - 1;
                ScrollTextUp(1);
            }
        }
    }

    public void ScrollTextUp(int lines)
    {
        if (lines <= 0 || lines > TextRows)
            throw new IOEmulatorException("Invalid number of lines to scroll.");
        
        int charHeight = ResolutionH / TextRows;
        int shiftPixels = lines * charHeight;
        int shiftBytes = shiftPixels * ResolutionW;
        
        Array.Copy(PixelBuffer, shiftBytes, PixelBuffer, 0, PixelBuffer.Length - shiftBytes);
        
        RGB bgColor = GetColor(BackgroundColorIndex);
        Array.Fill(PixelBuffer, bgColor, PixelBuffer.Length - shiftBytes, shiftBytes);
    }


    public void PutString(string str)
    {
        foreach (char ch in str)
        {
            PutChar(ch);
        }
    }

    public Glyph GetGlyphForCharacter(int character)
    {
        return CodePage.Glyphs[character];
    }

    public void PutGlyph(Glyph glyph)
    {
        var bg = GetColor(BackgroundColorIndex);
        var fg = GetColor(ForegroundColorIndex);
        PutGlyph(glyph, TurtleX, TurtleY, bg, fg);
    }

    public void PutGlyph(Glyph glyph, int x0, int y0, RGB bg, RGB fg)
    {
        if (glyph == null) return;
        glyph.Action?.Invoke();
        if (glyph.Bitmap == null || glyph.Bitmap.Length == 0) return;
        int glyphHeight = glyph.Height;
        int glyphWidth = glyph.Width;
        int i = 0;
        for (int y = 0; y < glyphHeight; y++)
        {
            int pixelY = y0 + y;
            // Skip whole row if outside vertical bounds, but keep bitmap index in sync
            if (pixelY < 0 || pixelY >= ResolutionH)
            {
                i += glyphWidth;
                continue;
            }
            for (int x = 0; x < glyphWidth; x++)
            {
                int pixelX = x0 + x;
                byte pixelValue = glyph.Bitmap[i++];
                if (pixelX < 0 || pixelX >= ResolutionW) continue;
                if (pixelValue == 0)
                {
                    WritePixelClipped(pixelX, pixelY, bg);
                }
                else
                {
                    WritePixelClipped(pixelX, pixelY, fg);
                }
            }
        }
    }

    public void PutGlyph(int character, int x0, int y0, RGB bg, RGB fg)
    {
        var glyph = GetGlyphForCharacter(character);
        PutGlyph(glyph, x0, y0, bg, fg);
    }

    public Coords GetTextXY(int col, int row)
    {
        int charWidth = ResolutionW / TextCols;
        int charHeight = ResolutionH / TextRows;
        return new Coords
        {
            X = col * charWidth,
            Y = row * charHeight
        };
    }

    public void PutGlyphAtCell(int character, int col, int row, RGB bg, RGB fg)
    {
        var coords = GetTextXY(col, row);
        PutGlyph(character, coords.X, coords.Y, bg, fg);
    }

    public void PutGlyphAtCursor(int character, RGB bg, RGB fg)
    {
        PutGlyphAtCell(character, CursorX, CursorY, bg, fg);
    }

    public void PutGlyphAtCursor(int character)
    {
        var bg = GetColor(BackgroundColorIndex);
        var fg = GetColor(ForegroundColorIndex);
        PutGlyphAtCell(character, CursorX, CursorY, bg, fg);
    }

    // ====== Primitive graphics ======
    public void PSet(int x, int y, int colorIndex)
    {
        WritePixelClipped(x, y, GetColor(colorIndex));
    }

    public RGB Point(int x, int y)
    {
        return ReadPixelClipped(x, y);
    }

    public void Line(int x1, int y1, int x2, int y2, int? colorIndex = null)
    {
        var color = colorIndex.HasValue ? GetColor(colorIndex.Value) : GetColor(ForegroundColorIndex);
        // Bresenham
        int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;
        int x = x1, y = y1;
        while (true)
        {
            WritePixelClipped(x, y, color);
            if (x == x2 && y == y2) break;
            int e2 = err << 1;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }
    }

    // World versions using WINDOW
    public void PSetW(double x, double y, int colorIndex)
    {
        var (sx, sy) = WorldToScreen(x, y);
        PSet(sx, sy, colorIndex);
    }

    public void LineW(double x1, double y1, double x2, double y2, int? colorIndex = null)
    {
        var (sx1, sy1) = WorldToScreen(x1, y1);
        var (sx2, sy2) = WorldToScreen(x2, y2);
        Line(sx1, sy1, sx2, sy2, colorIndex);
    }

    // ====== Block GET/PUT ======
    public enum RasterOp { PSET, AND, OR, XOR }

    public struct ImageBlock
    {
        public int Width;
        public int Height;
        public RGB[] Data;
    }

    public ImageBlock GetBlock(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0) throw new IOEmulatorException("Invalid block dimensions.");
        var data = new RGB[width * height];
        int i = 0;
        for (int yy = 0; yy < height; yy++)
        {
            int py = y + yy;
            for (int xx = 0; xx < width; xx++)
            {
                int px = x + xx;
                if ((uint)px < (uint)ResolutionW && (uint)py < (uint)ResolutionH)
                {
                    data[i++] = PixelBuffer[py * ResolutionW + px];
                }
                else
                {
                    data[i++] = new RGB(0, 0, 0);
                }
            }
        }
        return new ImageBlock { Width = width, Height = height, Data = data };
    }

    private static RGB ApplyOp(RGB dst, RGB src, RasterOp op)
    {
        byte f(byte a, byte b) => op switch
        {
            RasterOp.PSET => b,
            RasterOp.AND => (byte)(a & b),
            RasterOp.OR  => (byte)(a | b),
            RasterOp.XOR => (byte)(a ^ b),
            _ => b
        };
        return new RGB(f(dst.R, src.R), f(dst.G, src.G), f(dst.B, src.B));
    }

    public void PutBlock(int x, int y, in ImageBlock block, RasterOp op)
    {
        if (block.Data == null || block.Data.Length != block.Width * block.Height)
            throw new IOEmulatorException("Invalid block data.");
        int i = 0;
        for (int yy = 0; yy < block.Height; yy++)
        {
            int py = y + yy;
            if ((uint)py >= (uint)ResolutionH) { i += block.Width; continue; }
            for (int xx = 0; xx < block.Width; xx++)
            {
                int px = x + xx;
                var src = block.Data[i++];
                if ((uint)px >= (uint)ResolutionW) continue;
                if (IsClipped(px, py)) continue;
                var dst = PixelBuffer[py * ResolutionW + px];
                PixelBuffer[py * ResolutionW + px] = ApplyOp(dst, src, op);
            }
        }
    }

    // ====== BLOAD/BSAVE (VRAM bytes) ======
    public byte[] ReadVramBytes(int offset, int count) => VRAM.ReadBytes(offset, count);
    public void WriteVramBytes(int offset, ReadOnlySpan<byte> data) => VRAM.WriteBytes(offset, data);

    public void BSave(string path, int offset, int length)
    {
        var bytes = ReadVramBytes(offset, length);
        File.WriteAllBytes(path, bytes);
    }

    public void BLoad(string path, int offset)
    {
        var bytes = File.ReadAllBytes(path);
        WriteVramBytes(offset, bytes);
    }
}

public class IOEmulatorException : Exception
{
    public string Method;

    public IOEmulatorException(string message, [CallerMemberName] string method = "") : base(message)
    {
        Method = method;
    }

    public IOEmulatorException(string message, string method, Exception innerException) : base(message, innerException)
    {
        Method = method;
    }
}

public class ColorOutOfRangeException(int index, int length, [CallerMemberName] string method = "") :
    IOEmulatorException($"Color index out of range: {index}, {length}", method)
{
    public int Index = index;
    public int Length = length;
}