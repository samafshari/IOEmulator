using System.Runtime.CompilerServices;

namespace Neat;

public struct RGB(byte r, byte g, byte b)
{
    public byte R = r;
    public byte G = g;
    public byte B = b;
}

public struct DrawnCharacter
{
    public int BackgroundColorIndex;
    public int ForegroundColorIndex;
    public int CharacterCode;
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

public class IOEmulator
{
    public RGB[] Palette = [];
    public int TextCols;
    public int TextRows;
    public int ResolutionW;
    public int ResolutionH;
    public DrawnCharacter[] TextBuffer = [];
    public RGB[] PixelBuffer = [];
    public int BackgroundColorIndex;
    public int ForegroundColorIndex;
    public int CursorX;
    public int CursorY;

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

    public ref DrawnCharacter ReadCharAt(int col, int row)
    {
        if (col < 0 || col >= TextCols || row < 0 || row >= TextRows)
            throw new IOEmulatorException("Text coordinates out of range.");
        return ref TextBuffer[row * TextCols + col];
    }

    public int ReadTextAt(int col, int row)
    {
        if (col < 0 || col >= TextCols || row < 0 || row >= TextRows)
            throw new IOEmulatorException("Text coordinates out of range.");
        return TextBuffer[row * TextCols + col].CharacterCode;
    }

    public void WriteTextAt(int col, int row, int charCode)
    {
        WriteTextAt(col, row, charCode, ForegroundColorIndex, BackgroundColorIndex);
    }

    public void WriteTextAt(int col, int row, int charCode, int fgColorIndex, int bgColorIndex)
    {
        if (col < 0 || col >= TextCols || row < 0 || row >= TextRows)
            throw new IOEmulatorException("Text coordinates out of range.");
        TextBuffer[row * TextCols + col] = new DrawnCharacter
        {
            BackgroundColorIndex = bgColorIndex,
            ForegroundColorIndex = fgColorIndex,
            CharacterCode = charCode
        };
    }

    public void ClearPixelBuffer()
    {
        RGB bgColor = GetColor(BackgroundColorIndex);
        Array.Fill(PixelBuffer, bgColor);
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
        ClearPixelBuffer();
    }

    public void Cls()
    {
        ClearPixelBuffer();
        CursorX = 0;
        CursorY = 0;
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
        Array.Copy(TextBuffer, lines * TextCols, TextBuffer, 0, (TextRows - lines) * TextCols);
        // Clear the newly freed lines at the bottom
        for (int r = TextRows - lines; r < TextRows; r++)
        {
            for (int c = 0; c < TextCols; c++)
            {
                WriteTextAt(c, r, 32); // ASCII space
            }
        }
    }


    public void PutString(string str)
    {
        foreach (char ch in str)
        {
            PutChar(ch);
        }
    }

    public void PutGlyph(Glyph glyph)
    {
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
            if (y >= ResolutionH) break;
            for (int x = 0; x < glyphWidth; x++)
            {
                if (x >= ResolutionW) break;
                i++;
                byte pixelValue = glyph.Bitmap[i];
                int pixelX = x0 + x;
                int pixelY = y0 + y;
                if (pixelValue == 0)
                {
                    WritePixelAt(pixelX, pixelY, bg);
                }
                else
                {
                    WritePixelAt(pixelX, pixelY, fg);
                }
            }
        }
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