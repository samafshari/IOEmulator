using System;
using Xunit;
using Xunit.Abstractions;

namespace Neat.Test;

public class RaytraceExecutionTests
{
    private readonly ITestOutputHelper _output;

    public RaytraceExecutionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RAYTRACE_Loads_And_Starts_Without_Parse_Error()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        
        var src = QBasicSamples.Load("RAYTRACE");
        
        try
        {
            // This should not throw a parse/validation error
            // We'll cancel quickly since full execution takes a long time
            using var cts = new System.Threading.CancellationTokenSource(100);
            interp.Run(src, cts.Token);
            
            // If we get here without an exception (other than cancellation), parsing worked
            Assert.True(true);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Exception: {ex.GetType().Name}");
            _output.WriteLine($"Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                _output.WriteLine($"Inner: {ex.InnerException.Message}");
            }
            
            // Only rethrow if it's not a cancellation
            if (ex is not OperationCanceledException)
            {
                throw;
            }
        }
    }
    
    [Fact]
    public void RAYTRACE_Individual_Lines_Parse()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        
        // Test individual problematic patterns from RAYTRACE
        var tests = new[]
        {
            "SCREEN 13\nCLS\nIF DIST <= 0 THEN HIT = 1: GOTO MARCHEND\nMARCHEND:\nEND",
            "SCREEN 13\nCLS\nIF HIT = 0 THEN\n  COL = 0\n  GOTO NEXTPIX\nEND IF\nNEXTPIX:\nEND",
            "SCREEN 13\nCLS\nDOT = (NX * L2X + NY * L2Y + NZ * L2Z) / S\nEND",
            "SCREEN 13\nCLS\nCOL = DOT * 255 / S\nEND"
        };
        
        foreach (var src in tests)
        {
            try
            {
                interp.Run(src);
                _output.WriteLine($"✓ Passed: {src.Split('\n')[2].Substring(0, Math.Min(40, src.Split('\n')[2].Length))}...");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"✗ Failed: {src}");
                _output.WriteLine($"  Error: {ex.Message}");
                throw;
            }
        }
    }
}
