using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Tests;

public class WOLFComponentsTests
{
    [Fact]
    public void RayAngle_Mod_Normalization_Allows_Table_Indexing()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
DIM A(359) AS INTEGER
FOR I = 0 TO 359: A(I) = I: NEXT
RAYANG = 725
RAYANG = RAYANG MOD 360
IF RAYANG < 0 THEN RAYANG = RAYANG + 360
X = A(RAYANG)
IF X = 5 THEN PSET 0,0, 15
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(0, 0));
    }

    [Fact]
    public void Bounds_Check_With_OR_And_Nested_Else_Works()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
MW = 20: MH = 20
MX = -1: MY = 10
HIT = 0
IF MX < 0 OR MX >= MW OR MY < 0 OR MY >= MH THEN
  HIT = 1
ELSE
  IF 0 = 1 THEN HIT = 2
END IF
IF HIT = 1 THEN PSET 0,0, 15
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(0, 0));
    }
}
