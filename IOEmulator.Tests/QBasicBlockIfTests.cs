using System;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Test;

public class QBasicBlockIfTests
{
    [Fact]
    public void BlockIf_With_Else_Takes_Then_Branch()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
X = 1
IF X = 1 THEN
  PSET 5, 5, 15
ELSE
  PSET 5, 5, 10
END IF
";
        interp.Run(src);
  var c = io.ReadPixelAt(5, 5);
  Assert.Equal(15, c);
    }

    [Fact]
    public void BlockIf_With_Else_Takes_Else_Branch()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
X = 2
IF X = 1 THEN
  PSET 6, 6, 15
ELSE
  PSET 6, 6, 12
END IF
";
        interp.Run(src);
  var c = io.ReadPixelAt(6, 6);
  Assert.Equal(12, c);
    }

    [Fact]
    public void BlockIf_With_Inline_Else_Actions()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
X = 3
IF X = 1 THEN
  PSET 7, 7, 15
ELSE PSET 7, 7, 13
END IF
";
        interp.Run(src);
  var c = io.ReadPixelAt(7, 7);
  Assert.Equal(13, c);
    }

    [Fact]
    public void BlockIf_Else_With_Control_Transfer_Goto_Skips_After_EndIf()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
X = 0
IF X = 1 THEN
  PSET 20,20, 15
ELSE GOTO Done
END IF
PSET 20,20, 14
Done:
PSET 21,21, 12
";
        interp.Run(src);
        // 20,20 should remain background (skipped by GOTO), 21,21 should be set
  Assert.Equal(io.BackgroundColorIndex, io.ReadPixelAt(20,20));
  Assert.Equal(12, io.ReadPixelAt(21,21));
    }

    [Fact]
    public void BlockIf_With_AND_Condition_Works()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
A = 1: B = 2
IF A = 1 AND B = 2 THEN
  PSET 8,8, 10
END IF
";
        interp.Run(src);
  Assert.Equal(10, io.ReadPixelAt(8,8));
    }

    [Fact]
    public void Nested_BlockIfs_Work()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
A = 1: B = 2
IF A = 1 THEN
  IF B = 2 THEN
    PSET 10, 10, 15
  ELSE
    PSET 10, 10, 9
  END IF
ELSE
  PSET 10, 10, 8
END IF
";
        interp.Run(src);
  var c2 = io.ReadPixelAt(10, 10);
  Assert.Equal(15, c2);
    }
}
