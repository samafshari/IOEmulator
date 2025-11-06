using System;
using Xunit;

namespace Neat.Tests;

public class QBasicDoLoopTests
{
    [Fact]
    public void Do_While_TopChecked_Runs_While_True()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
DO WHILE I < 3
  PSET I, 0, 15
  I = I + 1
LOOP
";
        interp.Run(src);
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(0,0));
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(1,0));
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(2,0));
    }

    [Fact]
    public void For_With_Inline_ExitFor_Terminates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
FOR J = 0 TO 10
  IF J = 2 THEN EXIT FOR
  PSET J, 6, 15
NEXT J
PSET 10,10, 12
";
        interp.Run(src);
        // Only J=0 and J=1 should be set
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(0,6));
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(1,6));
        Assert.Equal(io.GetColor(io.BackgroundColorIndex), io.ReadPixelAt(2,6));
        Assert.Equal(io.GetColor(12), io.ReadPixelAt(10,10));
    }

    [Fact]
    public void Do_With_BlockIf_Else_ExitDo_Terminates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
DO
  I = I + 1
  IF I < 3 THEN
    ' keep looping
  ELSE
    EXIT DO
  END IF
LOOP
PSET 8,8, 15
";
        interp.Run(src);
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(8,8));
    }
    [Fact]
    public void Do_With_BlockIf_And_ExitDo_Terminates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
DO
  I = I + 1
  IF I = 3 THEN
    EXIT DO
  END IF
LOOP
PSET 12,12, 15
";
        interp.Run(src);
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(12,12));
    }

    [Fact]
    public void Do_While_TopChecked_Skips_When_False()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 1
DO WHILE I < 1
  PSET 5,5, 15
LOOP
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.Equal(bg, io.ReadPixelAt(5,5));
    }

    [Fact]
    public void Do_Infinite_Loop_With_ExitDo_Terminates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
DO
  I = I + 1
  IF I = 3 THEN EXIT DO
LOOP
PSET 10,10, 15
";
        interp.Run(src);
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(10,10));
    }

    [Fact]
    public void Do_Nested_ExitDo_Exits_Only_Innermost()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
DO
  DO
    EXIT DO
  LOOP
  I = I + 1
  EXIT DO
LOOP
PSET I,3, 15
";
        interp.Run(src);
        // After inner EXIT DO, outer loop continues to increment I, then exits; I should be 1
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(1,3));
    }

    [Fact]
    public void Do_Until_TopChecked_Skips_When_True()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
DO UNTIL I = 0
  PSET 5,5, 15
LOOP
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.Equal(bg, io.ReadPixelAt(5,5));
    }

    [Fact]
    public void Do_Loop_While_BottomChecked()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
DO
  PSET I, 1, 15
  I = I + 1
LOOP WHILE I < 2
";
        interp.Run(src);
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(0,1));
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(1,1));
    }

    [Fact]
    public void Do_Loop_Until_BottomChecked()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
DO
  I = I + 1
LOOP UNTIL I = 2
PSET 10,10, 15
";
        interp.Run(src);
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(10,10));
    }

    [Fact]
    public void Exit_Do_And_Exit_For_Work()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
FOR J = 0 TO 10
  DO
    PSET 3,3, 15
    EXIT DO
  LOOP
  EXIT FOR
NEXT
PSET 4,4, 12
";
        interp.Run(src);
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(3,3));
        Assert.Equal(io.GetColor(12), io.ReadPixelAt(4,4));
    }
}
