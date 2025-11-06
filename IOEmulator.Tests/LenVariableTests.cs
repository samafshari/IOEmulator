using System;
using Xunit;
using Xunit.Abstractions;

namespace Neat.Test;

public class LenVariableTests
{
    private readonly ITestOutputHelper _output;

    public LenVariableTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void LEN_AsVariableName_ShouldThrowError()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);

        var code = "LEN = 100\nEND";

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            interp.Run(code, default);
        });
        
        _output.WriteLine($"✓ Correctly threw error: {exception.Message}");
        Assert.Contains("reserved keyword", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEN", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LEN_FunctionVsVariable_Conflict()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);

        // LEN is a built-in function for string length
        // Using it as a variable might cause conflicts
        var code = @"
A$ = ""HELLO""
L = LEN(A$)
PRINT ""String length:""; L
LEN = 50
PRINT ""LEN variable:""; LEN
IF LEN = 0 THEN LEN = 1
PRINT ""After check:""; LEN
END
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var cts = new System.Threading.CancellationTokenSource(100);
            interp.Run(code, cts.Token);
        });
        _output.WriteLine($"✓ Correctly threw error: {ex.Message}");
        Assert.Contains("reserved keyword", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEN", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RAYTRACE_Simplified_WithLenVariable()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);

        // Simplified version of the RAYTRACE pattern
        var code = @"
SCREEN 13
S = 1000
RX = 100
RY = 200
RZ = 300
LEN = SQR(RX * RX + RY * RY + RZ * RZ)
PRINT ""Computed LEN:""; LEN
IF LEN = 0 THEN LEN = 1
PRINT ""Final LEN:""; LEN
END
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var cts = new System.Threading.CancellationTokenSource(100);
            interp.Run(code, cts.Token);
        });
        _output.WriteLine($"✓ Correctly threw error: {ex.Message}");
        Assert.Contains("reserved keyword", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEN", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RAYTRACE_ExactProblematicLine()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);

        // Extract the exact context from RAYTRACE.bas around line 51
        var code = @"
SCREEN 13
S = 1000
RX = 100
RY = 200
RZ = 300

' Normalize ray direction
LEN = SQR(RX * RX + RY * RY + RZ * RZ)
IF LEN = 0 THEN LEN = 1
RX = RX * S / LEN
RY = RY * S / LEN
RZ = RZ * S / LEN

PRINT ""Normalized RX:""; RX
PRINT ""Normalized RY:""; RY
PRINT ""Normalized RZ:""; RZ
END
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var cts = new System.Threading.CancellationTokenSource(100);
            interp.Run(code, cts.Token);
        });
        _output.WriteLine($"✓ Correctly threw error: {ex.Message}");
        Assert.Contains("reserved keyword", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEN", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LEN_InCondition_LeftSide()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);

        var code = @"
LEN = 0
IF LEN = 0 THEN PRINT ""LEN is zero""
LEN = 5
IF LEN = 0 THEN PRINT ""Should not print""
PRINT ""Done""
END
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var cts = new System.Threading.CancellationTokenSource(100);
            interp.Run(code, cts.Token);
        });
        _output.WriteLine($"✓ Correctly threw error: {ex.Message}");
        Assert.Contains("reserved keyword", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEN", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LEN_InlineAssignment_InTHEN()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);

        var code = @"
LEN = 0
PRINT ""Before:""; LEN
IF LEN = 0 THEN LEN = 1
PRINT ""After:""; LEN
IF LEN = 1 THEN PRINT ""Success""
END
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var cts = new System.Threading.CancellationTokenSource(100);
            interp.Run(code, cts.Token);
        });
        _output.WriteLine($"✓ Correctly threw error: {ex.Message}");
        Assert.Contains("reserved keyword", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEN", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RAYTRACE_FullProgram_WithDebugOutput()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);

        var code = QBasicSamples.Load("RAYTRACE.bas");

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(200); // Run for 200ms
            interp.Run(code, cts.Token);
            _output.WriteLine("✓ RAYTRACE.bas ran successfully for 200ms");
            Assert.True(true);
        }
        catch (OperationCanceledException)
        {
            _output.WriteLine("✓ RAYTRACE.bas cancelled as expected");
            Assert.True(true);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ ERROR in RAYTRACE.bas: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                _output.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    [Fact]
    public void LEN_Function_StillWorks()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);

        var code = @"
A$ = ""HELLO WORLD""
L = LEN(A$)
PRINT ""Length is:""; L
IF L = 11 THEN PRINT ""Correct length""
END
";

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(100);
            interp.Run(code, cts.Token);
            _output.WriteLine("✓ Test passed - LEN() function works");
            Assert.True(true);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ ERROR: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}
