using System;
using Xunit;

namespace Neat.Tests;

public class QBasicElseIfTests
{
    [Fact]
    public void ElseIf_First_Branch_Taken()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
X = 2
IF X = 1 THEN
  PSET 1,1, 9
ELSEIF X = 2 THEN
  PSET 1,1, 10
ELSE
  PSET 1,1, 11
END IF
";
        interp.Run(src);
        Assert.Equal(io.GetColor(10), io.ReadPixelAt(1,1));
    }

    [Fact]
    public void ElseIf_Second_Branch_Taken()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
X = 3
IF X = 1 THEN
  PSET 2,2, 9
ELSEIF X = 2 THEN
  PSET 2,2, 10
ELSEIF X = 3 THEN
  PSET 2,2, 12
ELSE
  PSET 2,2, 11
END IF
";
        interp.Run(src);
        Assert.Equal(io.GetColor(12), io.ReadPixelAt(2,2));
    }

    [Fact]
    public void ElseIf_Fallthrough_To_Else()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
X = 5
IF X = 1 THEN
  PSET 3,3, 9
ELSEIF X = 2 THEN
  PSET 3,3, 10
ELSE
  PSET 3,3, 14
END IF
";
        interp.Run(src);
        Assert.Equal(io.GetColor(14), io.ReadPixelAt(3,3));
    }

    [Fact]
    public void ElseIf_Branch_MultiLine_Executes_All()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
X = 2
IF X = 1 THEN
  PSET 4,4, 9
ELSEIF X = 2 THEN
  PSET 4,4, 11
  PSET 5,5, 12
ELSE
  PSET 4,4, 14
END IF
";
        interp.Run(src);
        Assert.Equal(io.GetColor(11), io.ReadPixelAt(4,4));
        Assert.Equal(io.GetColor(12), io.ReadPixelAt(5,5));
    }
}
