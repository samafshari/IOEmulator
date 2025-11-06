using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Tests;

public class QBasicErrorHandlingTests
{
    [Fact]
    public void Incomplete_SIN_In_Assignment_Prints_Error_And_Terminates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = "SCREEN 13\r\nX = SIN(\r\n";
        interp.Run(src);
        // Expect some pixels changed due to error message being printed
        var bg = io.GetColor(io.BackgroundColorIndex);
        bool anyDiff = false;
        for (int y = 0; y < io.ResolutionH && !anyDiff; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var c = io.PixelBuffer[y * io.ResolutionW + x];
                if (c.R != bg.R || c.G != bg.G || c.B != bg.B) { anyDiff = true; break; }
            }
        }
        Assert.True(anyDiff);
    }

    [Fact]
    public void Incomplete_PC_In_Function_Prints_Error_And_Terminates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = "SCREEN 13\r\nPSET PC(10, , 15\r\n"; // malformed: missing y before comma and missing ')'
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        bool anyDiff = false;
        for (int y = 0; y < io.ResolutionH && !anyDiff; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var c = io.PixelBuffer[y * io.ResolutionW + x];
                if (c.R != bg.R || c.G != bg.G || c.B != bg.B) { anyDiff = true; break; }
            }
        }
        Assert.True(anyDiff);
    }
}
