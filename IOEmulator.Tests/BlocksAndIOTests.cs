using System;
using System.IO;
using Xunit;
using Neat;

public class BlocksAndIOTests
{
    [Fact]
    public void GetPut_Xor_Mode_Works()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(16, 16);
        io.ResetView();
        // Draw a small square
        io.SetColor(4, new RGB(10, 20, 30));
        for (int y = 2; y < 6; y++)
            for (int x = 2; x < 6; x++) io.PSet(x, y, 4);
        var block = io.GetBlock(2, 2, 4, 4);
        // Put the same block overlapping with XOR -> should zero out where it overlaps
        io.PutBlock(2, 2, in block, IOEmulator.RasterOp.XOR);
        var c = io.Point(3, 3);
        Assert.Equal(0, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void BSave_BLoad_RoundTrip()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(8, 8);
        io.ResetView();
        io.SetColor(6, new RGB(128, 64, 32));
        io.PSet(1, 1, 6);
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            io.BSave(tmp, 0, io.VRAM.ByteLength);
            // Clear
            io.ClearPixelBuffer();
            // Load back
            io.BLoad(tmp, 0);
            var c = io.Point(1, 1);
            Assert.Equal(128, c.R);
            Assert.Equal(64, c.G);
            Assert.Equal(32, c.B);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
