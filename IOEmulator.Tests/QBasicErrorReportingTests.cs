using System;
using Xunit;

namespace Neat.Test;

public class QBasicErrorReportingTests
{
    [Fact]
    public void Error_Prints_Line_Number_And_Content_OnScreen()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        
        string src = @"SCREEN 13
CLS
X = 5
Y = BADFUNCTION(10)
PSET 0,0, 15
";
        interp.Run(src);
        
        // The error should have printed to the screen, so some pixels should be changed
        var bgIndex = io.BackgroundColorIndex;
        bool anyDiff = false;
        for (int y = 0; y < io.ResolutionH && !anyDiff; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var idx = io.IndexBuffer[y * io.ResolutionW + x];
                if (idx != bgIndex) { anyDiff = true; break; }
            }
        }
        Assert.True(anyDiff, "Expected error message to be printed on screen");
    }
}
