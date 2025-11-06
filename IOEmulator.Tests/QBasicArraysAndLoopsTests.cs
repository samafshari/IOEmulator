using System;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Test;

public class QBasicArraysAndLoopsTests
{
    [Fact(Timeout = 5000)]
    public async Task Dim1D_Array_Assignment_And_Read()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
DIM A(2) AS INTEGER
A(1) = 5
IF A(1) = 5 THEN PSET 0,0,15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(0, 0));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Dim2D_Array_Assignment_And_Read()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
DIM M(2,2) AS INTEGER
M(1,1) = 7
IF M(1,1) = 7 THEN PSET 1,0,15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(1, 0));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Data_Read_Into_Array_Works()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
DATA 3,4
DIM A(1) AS INTEGER
READ A(0)
READ A(1)
IF A(0) = 3 AND A(1) = 4 THEN PSET 2,0,15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(2, 0));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task While_With_AND_Stops_When_Second_Condition_Fails()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
I = 0
HIT = 0
WHILE I < 3 AND HIT = 0
I = I + 1
IF I = 2 THEN HIT = 1
WEND
IF I = 2 THEN PSET 3,0,15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(3, 0));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Mod_Keyword_Operator_Works()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = (7 MOD 4)
IF X = 3 THEN PSET 4,0,15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(4, 0));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task For_Next_Without_Variable_Works()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
SUM = 0
FOR I = 1 TO 5
  SUM = SUM + I
NEXT
IF SUM = 15 THEN PSET 5,0,15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(5, 0));
        });
    }
}
