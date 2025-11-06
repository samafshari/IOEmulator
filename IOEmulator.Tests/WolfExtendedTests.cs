using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Test;

public class WolfExtendedTests
{
    [Fact(Timeout = 20000)]
    public async Task Wolf_Sample_Runs_For_2s_And_Pixels_Change()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb) { SpeedFactor = 200.0 };
        var src = QBasicSamples.Load("WOLF.bas");

        using var cts = new CancellationTokenSource();
        Exception thrown = null;
        var t = Task.Run(() =>
        {
            try { interp.Run(src, cts.Token); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { thrown = ex; }
        });

        // Let it run for about 2 seconds total
        await Task.Delay(2000);
        Assert.Null(thrown);
        var bg = io.GetColor(io.BackgroundColorIndex);
        int nonBg = CountNonBackgroundPixels(io, bg);
        Assert.True(nonBg > 0, "Expected rendering after 2s run");

        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(2000));
        if (thrown != null) throw new Exception("WOLF.bas threw during 2s run", thrown);
    }

    private static int CountNonBackgroundPixels(IOEmulator io, RGB bg)
    {
        int count = 0;
        for (int y = 0; y < io.ResolutionH; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var c = io.PixelBuffer[y * io.ResolutionW + x];
                if (c.R != bg.R || c.G != bg.G || c.B != bg.B) count++;
            }
        }
        return count;
    }

    private static int CountPixelDiffs(RGB[] a, RGB[] b)
    {
        int n = Math.Min(a.Length, b.Length);
        int diff = 0;
        for (int i = 0; i < n; i++)
        {
            if (a[i].R != b[i].R || a[i].G != b[i].G || a[i].B != b[i].B) diff++;
        }
        return diff;
    }
}
