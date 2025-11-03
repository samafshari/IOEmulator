using System;
using Xunit;
using Neat;

public class PixelPrimitivesTests
{
    [Fact]
    public void PSet_And_Point_Work()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(64, 64);
        io.ResetView();
        io.BackgroundColorIndex = 0;
        io.SetColor(1, new RGB(255, 0, 0));
        io.PSet(10, 10, 1);
        var c = io.Point(10, 10);
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Line_Draws_Diagonal()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(32, 32);
        io.ResetView();
        io.SetColor(2, new RGB(0, 255, 0));
        io.ForegroundColorIndex = 2;
        io.Line(0, 0, 10, 10);
        var a = io.Point(0, 0);
        var b = io.Point(10, 10);
        Assert.Equal(0, a.R); Assert.Equal(255, a.G);
        Assert.Equal(0, b.R); Assert.Equal(255, b.G);
    }

    [Fact]
    public void View_Clipping_Restricts_Draw()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(32, 32);
        io.BackgroundColorIndex = 0;
        io.ClearPixelBuffer();
        io.SetColor(3, new RGB(0, 0, 255));
        io.ForegroundColorIndex = 3;
        io.SetView(8, 8, 15, 15);
        io.Line(0, 0, 31, 31);
        // Outside should remain background
        var outside = io.Point(2, 2);
        var inside = io.Point(10, 10);
        Assert.Equal(io.GetColor(io.BackgroundColorIndex).R, outside.R);
        Assert.Equal(255, inside.B);
    }
}
