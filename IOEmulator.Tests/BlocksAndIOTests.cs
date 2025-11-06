using System;
using System.IO;
using Xunit;
using Neat;

namespace Neat.Test;

public class BlocksAndIOTests
{
    [Fact]
    public void GetPut_Xor_Mode_Works()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(16, 16);
        io.ResetView();
        // Draw a small square (set palette index 4 to a distinct color)
        io.SetColor(4, unchecked((int)0xFF1E140A)); // AARRGGBB -> bytes (on LE) = BGRA: 0A 14 1E FF
        for (int y = 2; y < 6; y++)
            for (int x = 2; x < 6; x++) io.PSet(x, y, 4);
        var block = io.GetBlock(2, 2, 4, 4);
        // Put the same block overlapping with XOR -> should zero out where it overlaps
        io.PutBlock(2, 2, in block, IOEmulator.RasterOp.XOR);
    var idx = io.Point(3, 3);
    Assert.Equal(0, idx); // XOR of same block zeroes out to background index
    }

    [Fact]
    public void BSave_BLoad_RoundTrip()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(8, 8);
        io.ResetView();
    io.SetColor(6, unchecked((int)0xFF804020));
        io.PSet(1, 1, 6);
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            io.BSave(tmp, 0, io.VRAM.ByteLength);
            // Clear
            io.ClearPixelBuffer();
            // Load back
            io.BLoad(tmp, 0);
            var idx = io.Point(1, 1);
            Assert.Equal(6, idx);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
