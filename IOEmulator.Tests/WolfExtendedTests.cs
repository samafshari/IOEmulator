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
    int bgIdx = io.BackgroundColorIndex;
    int nonBg = CountNonBackgroundPixels(io, bgIdx);
        Assert.True(nonBg > 0, "Expected rendering after 2s run");

        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(2000));
        if (thrown != null) throw new Exception("WOLF.bas threw during 2s run", thrown);
    }

    private static int CountNonBackgroundPixels(IOEmulator io, int bgIdx)
    {
        int count = 0;
        for (int y = 0; y < io.ResolutionH; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var idx = io.IndexBuffer[y * io.ResolutionW + x];
                if (idx != bgIdx) count++;
            }
        }
        return count;
    }
    
}
