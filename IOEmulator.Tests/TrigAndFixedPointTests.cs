using System;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Tests;

public class TrigAndFixedPointTests
{
    [Fact]
    public void SIN_COS_Scale_Is_100()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
X = SIN(90)
Y = COS(0)
IF X = 100 AND Y = 100 THEN PSET 0,0, 15
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(0, 0));
    }

    [Fact]
    public void Mod_Normalization_Yields_0_359()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
A = -725: A = A MOD 360: IF A < 0 THEN A = A + 360
B = 725: B = B MOD 360: IF B < 0 THEN B = B + 360
IF A >= 0 AND A <= 359 AND B >= 0 AND B <= 359 THEN PSET 1,1, 15
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(1, 1));
    }

    [Fact]
    public void FixedPointStep_Moves_By_S_For_Cos0()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
S = 1000
RX = 0
DX = COS(0) ' 100
RX = RX + DX * S / 100
IF RX = 1000 THEN PSET 2,2, 15
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(2, 2));
    }
}
