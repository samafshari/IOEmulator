using System;
using System.Diagnostics;
using Xunit;

namespace Neat.Test;

public class SleepTests
{
    [Fact]
    public void Sleep_Fractional_Second_About_100ms_DefaultSpeed()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        var sw = Stopwatch.StartNew();
        string src = "SCREEN 13\nSLEEP 0.1\n";
        interp.Run(src);
        sw.Stop();
        // Allow generous bounds to avoid flakiness on CI
        Assert.InRange(sw.ElapsedMilliseconds, 50, 500);
    }

    [Fact]
    public void Sleep_Fractional_Second_Scales_With_SpeedFactor()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb) { SpeedFactor = 50.0 };
        var sw = Stopwatch.StartNew();
        string src = "SCREEN 13\nSLEEP 0.1\n";
        interp.Run(src);
        sw.Stop();
        // 0.1s / 50 => ~2ms; allow up to 100ms on slow machines
        Assert.InRange(sw.ElapsedMilliseconds, 0, 100);
    }
}
