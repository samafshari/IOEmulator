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

public class CodePage
{
    public int CharacterWidth;
    public int CharacterHeight;
    public int[][] GlyphBitmaps = []; // Each glyph bitmap is an array of integers representing rows of pixels
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

    public void ClearTextBuffer()
    {
        for (int i = 0; i < TextBuffer.Length; i++)
        {
            TextBuffer[i] = new DrawnCharacter
            {
                BackgroundColorIndex = BackgroundColorIndex,
                ForegroundColorIndex = ForegroundColorIndex,
                CharacterCode = 0
            };
        }
    }

    public void ClearPixelBuffer()
    {
        RGB bgColor = GetColor(BackgroundColorIndex);
        for (int i = 0; i < PixelBuffer.Length; i++)
        {
            PixelBuffer[i] = bgColor;
        }
    }

    public void SetTextDimensions(int width, int height)
    {
        TextCols = width;
        TextRows = height;
        TextBuffer = new DrawnCharacter[TextCols * TextRows];
        CursorX = 0;
        CursorY = 0;
        ClearTextBuffer();
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
        ClearTextBuffer();
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
        if (lines <= 0 || lines >= TextRows)
            throw new IOEmulatorException("Invalid number of lines to scroll.");
        Array.Copy(TextBuffer, lines * TextCols, TextBuffer, 0, (TextRows - lines) * TextCols);
        for (int r = TextRows - lines; r < TextRows; r++)
        {
            for (int c = 0; c < TextCols; c++)
            {
                WriteTextAt(c, r, 0);
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
}

public class IOEmulatorException : AggregateException
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