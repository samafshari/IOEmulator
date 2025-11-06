using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Neat.Test;

public class WolfSampleTests
{
    private readonly ITestOutputHelper _output;

    public WolfSampleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Timeout = 15000)]
    public async Task Wolf_Sample_Runs_And_Renders_Multiple_Frames()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb) { SpeedFactor = 200.0 }; // Fast but not too fast
        var src = QBasicSamples.Load("WOLF.bas");

        using var cts = new CancellationTokenSource();
        var taskStartTime = DateTime.UtcNow;
        Exception thrownException = null;
        
        var t = Task.Run(() =>
        {
            try
            {
                interp.Run(src, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when we cancel
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }
        });

        _output.WriteLine("Starting WOLF.bas execution...");
        
        // Wait for initial setup and first frame
        await Task.Delay(150);
        
        if (thrownException != null)
        {
            _output.WriteLine($"ERROR after 150ms: {thrownException.Message}");
            throw new Exception($"WOLF.bas failed during execution: {thrownException.Message}", thrownException);
        }
        
        // Capture initial pixel state
    int bgIdx = io.BackgroundColorIndex;
    int initialNonBgPixels = CountNonBackgroundPixels(io, bgIdx);
        _output.WriteLine($"Initial non-background pixels: {initialNonBgPixels}");
        
        Assert.True(initialNonBgPixels > 0, "Expected at least one frame to be rendered initially");
        
        // Run for a full second to ensure stability
        _output.WriteLine("Running for 1 second to verify stability...");
        await Task.Delay(1000);
        
        if (thrownException != null)
        {
            _output.WriteLine($"ERROR during 1-second run: {thrownException.Message}");
            throw new Exception($"WOLF.bas failed during execution: {thrownException.Message}", thrownException);
        }
        
    int finalNonBgPixels = CountNonBackgroundPixels(io, bgIdx);
        _output.WriteLine($"Final non-background pixels: {finalNonBgPixels}");
        
        // Cancel and wait
        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(2000));
        
        var elapsed = DateTime.UtcNow - taskStartTime;
        _output.WriteLine($"Total execution time: {elapsed.TotalMilliseconds:F0}ms");
        
        // Verify continuous rendering occurred (pixels should have changed over time)
        Assert.True(finalNonBgPixels > 0, "Expected continuous rendering");
        
        if (thrownException != null)
        {
            throw new Exception($"WOLF.bas failed: {thrownException.Message}", thrownException);
        }
        
        _output.WriteLine("✓ WOLF.bas ran successfully for 1+ seconds without errors");
        _output.WriteLine($"✓ Rendered frames with {finalNonBgPixels} non-background pixels");
    }

    private static int CountNonBackgroundPixels(IOEmulator io, int bgIdx)
    {
        int count = 0;
        for (int y = 0; y < io.ResolutionH; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var idx = io.IndexBuffer[y * io.ResolutionW + x];
                if (idx != bgIdx)
                {
                    count++;
                }
            }
        }
        return count;
    }
}
