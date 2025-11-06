using Xunit;
using Neat;

namespace Neat.Test;

public class TextAndWindowTests
{
    [Fact]
    public void WriteTextAt_Changes_Cell_Pixels()
    {
        var io = new IOEmulator();
        io.LoadQBasicScreenMode(0); // 320x200; 40x25 text
        io.BackgroundColorIndex = 0;
        io.ForegroundColorIndex = 7; // typically light gray/white
        io.ClearPixelBuffer();
        // capture background of top-left cell
    var before = io.Point(0, 0); // palette index
        io.WriteTextAt(0, 0, 'A');
        // some pixel in the cell should differ from background
        var cell = io.GetTextXY(0, 0);
        bool anyDifferent = false;
        for (int y = 0; y < 8 && !anyDifferent; y++)
            for (int x = 0; x < 8 && !anyDifferent; x++)
                anyDifferent |= io.Point(cell.X + x, cell.Y + y) != before;
        Assert.True(anyDifferent);
    }

    [Fact]
    public void Window_Transforms_To_View()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(100, 100);
        io.SetView(10, 10, 90, 90);
        io.SetWindow(-1, -1, 1, 1);
        var (sx, sy) = io.WorldToScreen(0, 0);
        Assert.InRange(sx, 49, 51);
        Assert.InRange(sy, 49, 51);
    }
}
