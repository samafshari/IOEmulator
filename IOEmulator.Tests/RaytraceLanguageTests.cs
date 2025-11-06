using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Test;

public class RaytraceLanguageTests
{
    [Fact]
    public void Nested_For_Loops_Draw_Grid()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        var src = QBasicSamples.Load("RAYTRACE_LOOPS");
        interp.Run(src);
        var on = io.GetColor(15);
        var bg = io.GetColor(io.BackgroundColorIndex);
        // Top-left 10x10 should be lit
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                Assert.Equal(on, io.ReadPixelAt(x, y));
            }
        }
        // A few outside samples remain background
        Assert.Equal(bg, io.ReadPixelAt(20, 0));
        Assert.Equal(bg, io.ReadPixelAt(0, 20));
    }

    [Fact]
    public void Inline_If_With_Multiple_Actions_And_Goto_Works()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        var src = QBasicSamples.Load("RAYTRACE_INLINE_GOTO");
        interp.Run(src);
        var on = io.GetColor(15);
        var bg = io.GetColor(io.BackgroundColorIndex);
        // Pixel at 2,2 should be set; 0,0 should remain background due to GOTO skipping it
        Assert.Equal(on, io.ReadPixelAt(2, 2));
        Assert.Equal(bg, io.ReadPixelAt(0, 0));
    }

    [Fact]
    public void Arithmetic_SQR_Precedence_Is_Correct()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        var src = QBasicSamples.Load("RAYTRACE_MATH_SQR");
        interp.Run(src);
        var on = io.GetColor(15);
        Assert.Equal(on, io.ReadPixelAt(5, 0));
    }

    [Fact]
    public void Parentheses_And_Division_Order_For_Dot_Expr()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
S = 10
NX = 10: NY = 0: NZ = 0
L2X = 10: L2Y = 0: L2Z = 0
DOT = (NX * L2X + NY * L2Y + NZ * L2Z) / S
PSET DOT, 1, 15
END
";
        interp.Run(src);
        var on = io.GetColor(15);
        Assert.Equal(on, io.ReadPixelAt(10, 1));
    }

    [Fact]
    public void Goto_Exits_For_Loop_Early()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
FOR STP = 1 TO 10
  IF STP = 3 THEN GOTO DONE
  I = I + 1
NEXT STP
DONE:
PSET I, 3, 15
END
";
        interp.Run(src);
        var on = io.GetColor(15);
        Assert.Equal(on, io.ReadPixelAt(2, 3)); // I incremented for STP=1,2 then jumped out
    }

    [Fact]
    public void Negative_Dot_Is_Clamped_To_Zero()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
S = 10
NX = -10: NY = 0: NZ = 0
L2X = 10: L2Y = 0: L2Z = 0
DOT = (NX * L2X + NY * L2Y + NZ * L2Z) / S
IF DOT < 0 THEN DOT = 0
PSET DOT, 4, 15
END
";
        interp.Run(src);
        var on = io.GetColor(15);
        Assert.Equal(on, io.ReadPixelAt(0, 4));
    }
}
