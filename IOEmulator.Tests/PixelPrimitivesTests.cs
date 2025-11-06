using System;
using Xunit;
using Neat;

namespace Neat.Test;

public class PixelPrimitivesTests
{
    [Fact]
    public void PSet_And_Point_Work()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(64, 64);
        io.ResetView();
        io.BackgroundColorIndex = 0;
    int red = (255 << 24) | (0 << 16) | (0 << 8) | 255; // ABGR
    io.SetColor(1, red);
        io.PSet(10, 10, 1);
    var c = io.Point(10, 10);
    Assert.Equal(1, c);
    }

    [Fact]
    public void Line_Draws_Diagonal()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(32, 32);
        io.ResetView();
    int green = (255 << 24) | (0 << 16) | (255 << 8) | 0;
    io.SetColor(2, green);
        io.ForegroundColorIndex = 2;
        io.Line(0, 0, 10, 10);
    var a = io.Point(0, 0);
    var b = io.Point(10, 10);
    Assert.Equal(2, a);
    Assert.Equal(2, b);
    }

    [Fact]
    public void View_Clipping_Restricts_Draw()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(32, 32);
        io.BackgroundColorIndex = 0;
        io.ClearPixelBuffer();
        int blue = (255 << 24) | (255 << 16) | (0 << 8) | 0;
        io.SetColor(3, blue);
        io.ForegroundColorIndex = 3;
        io.SetView(8, 8, 15, 15);
        io.Line(0, 0, 31, 31);
        // Outside should remain background
        var outside = io.Point(2, 2);
        var inside = io.Point(10, 10);
        Assert.Equal(io.BackgroundColorIndex, outside);
        Assert.Equal(3, inside);
    }
}
