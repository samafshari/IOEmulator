using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Neat.Tests;

public class QBasicSamplesIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public QBasicSamplesIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Timeout = 2000)]
    public async Task FRACTAL_Renders_And_Runs_Without_Errors()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb) { SpeedFactor = 300.0 };
        var src = QBasicSamples.Load("FRACTAL.bas");

        using var cts = new CancellationTokenSource();
        Exception thrownException = null;
        
        var t = Task.Run(() =>
        {
            try
            {
                interp.Run(src, cts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { thrownException = ex; }
        });

        _output.WriteLine("Starting FRACTAL.bas execution...");
        
        await Task.Delay(300);
        
        if (thrownException != null)
        {
            throw new Exception($"FRACTAL.bas failed: {thrownException.Message}", thrownException);
        }
        
        var bg = io.GetColor(io.BackgroundColorIndex);
        int pixelCount = CountNonBackgroundPixels(io, bg);
        _output.WriteLine($"Rendered pixels: {pixelCount}");
        
        Assert.True(pixelCount > 100, $"Expected significant rendering, got {pixelCount} pixels");
        
        // Run for 1 second total
        await Task.Delay(700);
        
        if (thrownException != null)
        {
            throw new Exception($"FRACTAL.bas failed during execution: {thrownException.Message}", thrownException);
        }
        
        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(1000));
        
        _output.WriteLine("✓ FRACTAL.bas ran successfully for 1+ seconds");
    }

    [Fact(Timeout = 2000)]
    public async Task LINES_Renders_And_Runs_Without_Errors()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb) { SpeedFactor = 300.0 };
        var src = QBasicSamples.Load("LINES.bas");

        using var cts = new CancellationTokenSource();
        Exception thrownException = null;
        
        var t = Task.Run(() =>
        {
            try
            {
                interp.Run(src, cts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { thrownException = ex; }
        });

        _output.WriteLine("Starting LINES.bas execution...");
        
        await Task.Delay(300);
        
        if (thrownException != null)
        {
            throw new Exception($"LINES.bas failed: {thrownException.Message}", thrownException);
        }
        
        var bg = io.GetColor(io.BackgroundColorIndex);
        int pixelCount = CountNonBackgroundPixels(io, bg);
        _output.WriteLine($"Rendered pixels: {pixelCount}");
        
        Assert.True(pixelCount > 100, $"Expected lines to be drawn, got {pixelCount} pixels");
        
        // Run for 1 second total
        await Task.Delay(700);
        
        if (thrownException != null)
        {
            throw new Exception($"LINES.bas failed during execution: {thrownException.Message}", thrownException);
        }
        
        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(1000));
        
        _output.WriteLine("✓ LINES.bas ran successfully for 1+ seconds");
    }

    [Fact(Timeout = 2000)]
    public async Task RAYTRACE_Renders_And_Runs_Without_Errors()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb) { SpeedFactor = 300.0 };
        var src = QBasicSamples.Load("RAYTRACE.bas");

        using var cts = new CancellationTokenSource();
        Exception thrownException = null;
        
        var t = Task.Run(() =>
        {
            try
            {
                interp.Run(src, cts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { thrownException = ex; }
        });

        _output.WriteLine("Starting RAYTRACE.bas execution...");
        
        await Task.Delay(300);
        
        if (thrownException != null)
        {
            throw new Exception($"RAYTRACE.bas failed: {thrownException.Message}", thrownException);
        }
        
        var bg = io.GetColor(io.BackgroundColorIndex);
        int pixelCount = CountNonBackgroundPixels(io, bg);
        _output.WriteLine($"Rendered pixels: {pixelCount}");
        
        Assert.True(pixelCount > 100, $"Expected sphere rendering, got {pixelCount} pixels");
        
        // Run for 1 second total
        await Task.Delay(700);
        
        if (thrownException != null)
        {
            throw new Exception($"RAYTRACE.bas failed during execution: {thrownException.Message}", thrownException);
        }
        
        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(1000));
        
        _output.WriteLine("✓ RAYTRACE.bas ran successfully for 1+ seconds");
    }

    private static int CountNonBackgroundPixels(IOEmulator io, RGB bg)
    {
        int count = 0;
        for (int y = 0; y < io.ResolutionH; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var c = io.PixelBuffer[y * io.ResolutionW + x];
                if (c.R != bg.R || c.G != bg.G || c.B != bg.B)
                {
                    count++;
                }
            }
        }
        return count;
    }
}
