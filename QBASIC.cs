using System;

namespace Neat;

// Minimal QBASIC-like facade over IOEmulator
public class QBasicApi
{
    private readonly IOEmulator io;

    public QBasicApi(IOEmulator emulator)
    {
        io = emulator;
    }

    // SCREEN mode selection
    public void SCREEN(int mode)
    {
        io.LoadQBasicScreenMode(mode);
        io.ResetView();
        io.ResetWindow();
    }

    // COLOR fg[, bg]
    public void COLOR(int fg, int? bg = null)
    {
        io.ForegroundColorIndex = fg;
        if (bg.HasValue) io.BackgroundColorIndex = bg.Value;
    }

    // LOCATE row, col
    public void LOCATE(int row, int col)
    {
        io.LocateCursor(col, row);
    }

    public void PRINT(string s)
    {
        io.PutString(s);
    }

    public void CLS()
    {
        io.Cls();
    }

    // VIEW (pixel coordinates)
    public void VIEW(int x1, int y1, int x2, int y2)
    {
        io.SetView(x1, y1, x2, y2);
    }

    public void VIEW()
    {
        io.ResetView();
    }

    // WINDOW (world coordinates)
    public void WINDOW(double wx1, double wy1, double wx2, double wy2)
    {
        io.SetWindow(wx1, wy1, wx2, wy2);
    }

    public void WINDOW()
    {
        io.ResetWindow();
    }

    // Graphics primitives
    public void PSET(int x, int y, int color) => io.PSet(x, y, color);
    public RGB POINT(int x, int y) => io.Point(x, y);
    public void LINE(int x1, int y1, int x2, int y2, int? color = null) => io.Line(x1, y1, x2, y2, color);

    // GET/PUT
    public IOEmulator.ImageBlock GET(int x, int y, int width, int height) => io.GetBlock(x, y, width, height);
    public void PUT(int x, int y, in IOEmulator.ImageBlock block, IOEmulator.RasterOp op = IOEmulator.RasterOp.PSET)
        => io.PutBlock(x, y, block, op);

    // BLOAD/BSAVE
    public void BLOAD(string path, int offset) => io.BLoad(path, offset);
    public void BSAVE(string path, int offset, int length) => io.BSave(path, offset, length);
}
