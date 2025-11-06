using System;
using Xunit;

namespace Neat.Tests;

public class QBasicForExitTests
{
    [Fact]
    public void Exit_For_Leaves_Only_Innermost_Loop()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,0
CLS
FOR I = 0 TO 2
  FOR J = 0 TO 10
    PSET I, 0, 15
    EXIT FOR
  NEXT
NEXT
";
        interp.Run(src);
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(0,0));
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(1,0));
        Assert.Equal(io.GetColor(15), io.ReadPixelAt(2,0));
    }
}
