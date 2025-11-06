using System;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Neat.Test;

public class LineByLineExecutionTests
{
    private readonly ITestOutputHelper _output;

    public LineByLineExecutionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RAYTRACE_StepThroughEachLine_ShouldNotThrow()
    {
        var code = QBasicSamples.Load("RAYTRACE.bas");
        StepThroughLines(code, "RAYTRACE.bas");
    }

    [Fact]
    public void WOLF_StepThroughEachLine_ShouldNotThrow()
    {
        var code = QBasicSamples.Load("WOLF.bas");
        StepThroughLines(code, "WOLF.bas");
    }

    private void StepThroughLines(string code, string fileName)
    {
        var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        _output.WriteLine($"Testing {fileName} - Total lines: {lines.Length}");
        _output.WriteLine("---");

        var io = new IOEmulator();
        var qb = new QBasicApi(io);

        // Accumulate lines one by one
        for (int i = 0; i < lines.Length; i++)
        {
            var currentLine = lines[i].TrimEnd();
            var lineNumber = i + 1;

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(currentLine))
            {
                _output.WriteLine($"Line {lineNumber}: (empty) - SKIPPED");
                continue;
            }

            _output.WriteLine($"Line {lineNumber}: {currentLine}");

            try
            {
                // Attempt to load the program up to this line
                var programUpToHere = string.Join("\n", lines.Take(i + 1));

                // Auto-close any open control blocks and inject any missing label stubs so partial programs validate
                var autoClosers = GenerateAutoClosers(programUpToHere);
                var labelStubs = GenerateLabelStubs(programUpToHere);
                var toRun = programUpToHere;
                if (!string.IsNullOrWhiteSpace(labelStubs)) toRun += "\n" + labelStubs;
                if (!string.IsNullOrWhiteSpace(autoClosers)) toRun += "\n" + autoClosers;
                toRun += "\nEND";

                try
                {
                    var testInterpreter = new QBasicInterpreter(qb);
                    // Try to run it briefly just to validate parsing
                    using var cts = new CancellationTokenSource(1);
                    testInterpreter.Run(toRun, cts.Token);
                    _output.WriteLine($"  ✓ Parsed successfully (program up to line {lineNumber})");
                }
                catch (OperationCanceledException)
                {
                    // Expected - we just want to validate parsing
                    _output.WriteLine($"  ✓ Parsed successfully (program up to line {lineNumber})");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"  ✗ PARSE ERROR: {ex.Message}");
                    throw new Exception($"Parse error at line {lineNumber} in {fileName}: {currentLine}", ex);
                }
            }
            catch (Exception ex) when (ex.Message.Contains("Parse error"))
            {
                throw; // Re-throw parse errors
            }
            catch (Exception ex)
            {
                _output.WriteLine($"  ⚠ Other error (may be expected): {ex.Message}");
            }
        }

        _output.WriteLine("---");
        _output.WriteLine($"✓ All {lines.Length} lines parsed successfully");
    }

    private static string GenerateAutoClosers(string upToSource)
    {
        // Build a lightweight structure stack to mirror validator expectations
    var stack = new System.Collections.Generic.Stack<(string kind, string arg)>();

        foreach (var raw in upToSource.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("'")) continue; // comment
            if (line.StartsWith("REM ", StringComparison.OrdinalIgnoreCase)) continue;

            // Remove leading label if present (e.g., LABEL: <code>)
            string code = line;
            int j = 0;
            while (j < code.Length && (char.IsLetterOrDigit(code[j]) || code[j] == '_')) j++;
            if (j > 0 && j < code.Length && code[j] == ':')
                code = code[(j + 1)..].Trim();
            else if (code.EndsWith(":"))
                code = string.Empty;

            if (string.IsNullOrWhiteSpace(code)) continue;

            foreach (var stmt in SplitStatementsPreserveIf(code))
            {
                var tokens = Tokenize(stmt);
                if (tokens.Count == 0) continue;
                var head = tokens[0].ToUpperInvariant();

                switch (head)
                {
                    case "FOR":
                        // capture loop var if available to emit NEXT var
                        string varName = tokens.Count > 1 ? tokens[1] : null;
                        stack.Push(("FOR", varName));
                        break;
                    case "NEXT":
                        if (stack.Count > 0 && stack.Peek().kind == "FOR") stack.Pop();
                        break;
                    case "WHILE":
                        stack.Push(("WHILE", null));
                        break;
                    case "WEND":
                        if (stack.Count > 0 && stack.Peek().kind == "WHILE") stack.Pop();
                        break;
                    case "IF":
                        if (IsBlockIfTokens(tokens)) stack.Push(("IF", null));
                        break;
                    case "END":
                        if (tokens.Count > 1 && tokens[1].Equals("IF", StringComparison.OrdinalIgnoreCase))
                        {
                            if (stack.Count > 0 && stack.Peek().kind == "IF") stack.Pop();
                        }
                        else if (tokens.Count > 1 && tokens[1].Equals("SELECT", StringComparison.OrdinalIgnoreCase))
                        {
                            if (stack.Count > 0 && stack.Peek().kind == "SELECT") stack.Pop();
                        }
                        break;
                    case "ENDIF":
                        if (stack.Count > 0 && stack.Peek().kind == "IF") stack.Pop();
                        break;
                    case "DO":
                        stack.Push(("DO", null));
                        break;
                    case "LOOP":
                        if (stack.Count > 0 && stack.Peek().kind == "DO") stack.Pop();
                        break;
                    case "SELECT":
                        if (tokens.Count > 1 && tokens[1].Equals("CASE", StringComparison.OrdinalIgnoreCase))
                            stack.Push(("SELECT", null));
                        break;
                }
            }
        }

        // Unwind the stack and emit proper closers in reverse order
        var sb = new System.Text.StringBuilder();
        foreach (var frame in stack)
        {
            // We'll collect to a temp list to reverse since Stack enumerates LIFO; but we need to close in LIFO order
        }
        var frames = stack.ToArray(); // LIFO -> array with top at index 0
        for (int i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            switch (frame.kind)
            {
                case "IF": sb.AppendLine("END IF"); break;
                case "WHILE": sb.AppendLine("WEND"); break;
                case "DO": sb.AppendLine("LOOP"); break;
                case "FOR": sb.AppendLine(frame.arg != null ? $"NEXT {frame.arg}" : "NEXT"); break;
                case "SELECT": sb.AppendLine("END SELECT"); break;
            }
        }
        return sb.ToString().TrimEnd();
    }

    private static System.Collections.Generic.List<string> Tokenize(string code)
    {
        var tokens = new System.Collections.Generic.List<string>();
        int i = 0;
        while (i < code.Length)
        {
            char c = code[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '"')
            {
                int j = i + 1;
                while (j < code.Length)
                {
                    if (code[j] == '"') { j++; break; }
                    j++;
                }
                tokens.Add(code[i..j]);
                i = j; continue;
            }
            if (",()-:;=+*/<>%".IndexOf(c) >= 0) { tokens.Add(c.ToString()); i++; continue; }
            int k = i;
            while (k < code.Length && !char.IsWhiteSpace(code[k]) && ",()-:;=+*/<>%".IndexOf(code[k]) < 0) k++;
            tokens.Add(code[i..k]);
            i = k;
        }
        // merge <=, >=, <>
        for (int t = 0; t < tokens.Count - 1; t++)
        {
            if (tokens[t] == "<" && tokens[t + 1] == ">") { tokens[t] = "<>"; tokens.RemoveAt(t + 1); t--; continue; }
            if (tokens[t] == "<" && tokens[t + 1] == "=") { tokens[t] = "<="; tokens.RemoveAt(t + 1); t--; continue; }
            if (tokens[t] == ">" && tokens[t + 1] == "=") { tokens[t] = ">="; tokens.RemoveAt(t + 1); t--; continue; }
        }
        return tokens;
    }

    private static bool IsBlockIfTokens(System.Collections.Generic.List<string> tokens)
    {
        if (tokens.Count == 0) return false;
        if (!tokens[0].Equals("IF", StringComparison.OrdinalIgnoreCase)) return false;
        int thenPos = -1;
        for (int i = 1; i < tokens.Count; i++)
        {
            if (tokens[i].Equals("THEN", StringComparison.OrdinalIgnoreCase)) { thenPos = i; break; }
        }
        return thenPos >= 0 && thenPos == tokens.Count - 1;
    }

    private static System.Collections.Generic.IEnumerable<string> SplitStatementsPreserveIf(string code)
    {
        // Similar to validator: don't split a block IF line; otherwise split on ':' outside strings
        var trimmed = code.Trim();
        if (trimmed.StartsWith("IF ", StringComparison.OrdinalIgnoreCase))
        {
            // Determine if it's a block IF (ends with THEN)
            var tokens = Tokenize(trimmed);
            if (IsBlockIfTokens(tokens)) { yield return trimmed; yield break; }
        }
        bool inStr = false;
        int start = 0;
        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];
            if (c == '"') inStr = !inStr;
            else if (c == ':' && !inStr)
            {
                var part = code.Substring(start, i - start).Trim();
                if (part.Length > 0) yield return part;
                start = i + 1;
            }
        }
        var last = code.Substring(start).Trim();
        if (last.Length > 0) yield return last;
    }

    private static string GenerateLabelStubs(string upToSource)
    {
        // Find GOTO <label> references (non-numeric) and add stub definitions if not already present
        var defined = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var referenced = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in upToSource.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("'")) continue;
            if (line.StartsWith("REM ", StringComparison.OrdinalIgnoreCase)) continue;

            // capture defined labels (prefix before ':')
            int j = 0;
            while (j < line.Length && (char.IsLetterOrDigit(line[j]) || line[j] == '_')) j++;
            if (j > 0 && j < line.Length && line[j] == ':')
            {
                var label = line[..j].Trim();
                if (!string.IsNullOrEmpty(label)) defined.Add(label);
            }

            // scan for GOTO tokens
            foreach (var stmt in SplitStatementsPreserveIf(line))
            {
                var tokens = Tokenize(stmt);
                if (tokens.Count >= 2 && tokens[0].Equals("GOTO", StringComparison.OrdinalIgnoreCase))
                {
                    var target = tokens[1];
                    if (!int.TryParse(target, out _)) referenced.Add(target);
                }
            }
        }

        var missing = referenced.Where(t => !defined.Contains(t)).ToList();
        if (missing.Count == 0) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var lbl in missing) sb.AppendLine(lbl + ":");
        return sb.ToString().TrimEnd();
    }

    [Fact]
    public void RAYTRACE_ExecuteUntilError_WithDetailedLogging()
    {
        var code = QBasicSamples.Load("RAYTRACE.bas");
        ExecuteWithDetailedLogging(code, "RAYTRACE.bas");
    }

    [Fact]
    public void WOLF_ExecuteUntilError_WithDetailedLogging()
    {
        var code = QBasicSamples.Load("WOLF.bas");
        ExecuteWithDetailedLogging(code, "WOLF.bas");
    }

    private void ExecuteWithDetailedLogging(string code, string fileName)
    {
        _output.WriteLine($"Executing {fileName} with detailed logging");
        _output.WriteLine("---");

        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interpreter = new QBasicInterpreter(qb);

        try
        {
            _output.WriteLine("✓ Program ready to run");

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Run for just 100ms

            try
            {
                interpreter.Run(code, cts.Token);
                _output.WriteLine("✓ Program ran without errors");
            }
            catch (OperationCanceledException)
            {
                _output.WriteLine("✓ Program cancelled after timeout (expected)");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"✗ RUNTIME ERROR: {ex.Message}");
                _output.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Runtime error in {fileName}: {ex.Message}", ex);
            }
        }
        catch (Exception ex) when (ex.Message.Contains("Runtime error"))
        {
            throw; // Re-throw runtime errors
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ ERROR: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new Exception($"Error in {fileName}: {ex.Message}", ex);
        }
    }

    [Fact]
    public void RAYTRACE_ParseSpecificProblematicPatterns()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);

        // Test specific patterns that might cause "Unexpected end of line"
        var patterns = new[]
        {
            "SCREEN 13",
            "COLOR 15, 0",
            "S = 1000",
            "FOR Y = 0 TO 199",
            "FOR X = 0 TO 319",
            "IF DIST <= 0 THEN HIT = 1: GOTO MARCHEND",
            "PosX = PosX + RX * DIST / S",
            "IF HIT = 0 THEN\n  COL = 0\n  GOTO NEXTPIX\nEND IF",
            "DOT = (NX * L2X + NY * L2Y + NZ * L2Z) / S",
            "IF DOT < 0 THEN DOT = 0",
            "COL = DOT * 255 / S",
            "PSET X, Y, COL",
            "NEXT X",
            "NEXT Y",
        };

        foreach (var pattern in patterns)
        {
            _output.WriteLine($"Testing pattern: {pattern.Replace("\n", "\\n")}");
            var interpreter = new QBasicInterpreter(qb);

            try
            {
                var testCode = pattern;
                // Provide minimal context when testing isolated NEXT statements
                var trimmed = pattern.TrimStart();
                if (trimmed.StartsWith("NEXT", StringComparison.OrdinalIgnoreCase))
                // Identify loop variable if present
                {
                    var tokens = Tokenize(trimmed);
                    string loopVar = tokens.Count > 1 ? tokens[1] : "I";
                    testCode = $"FOR {loopVar} = 0 TO 1\n" + testCode;
                }
        var labelStubs = GenerateLabelStubs(testCode);
        if (!string.IsNullOrWhiteSpace(labelStubs)) testCode += "\n" + labelStubs;
        var closers = GenerateAutoClosers(testCode);
        if (!string.IsNullOrWhiteSpace(closers)) testCode += "\n" + closers;

                using var cts = new CancellationTokenSource(1);
                interpreter.Run(testCode + "\nEND", cts.Token);
                _output.WriteLine("  ✓ Parsed successfully");
            }
            catch (OperationCanceledException)
            {
                _output.WriteLine("  ✓ Parsed successfully");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"  ✗ ERROR: {ex.Message}");
                throw new Exception($"Failed to parse pattern: {pattern}", ex);
            }
        }
    }

    [Fact]
    public void WOLF_ParseSpecificProblematicPatterns()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);

        // Test specific patterns from WOLF.bas
        var patterns = new[]
        {
            "DIM MAZE(MW-1, MH-1) AS INTEGER",
            "FOR I = 0 TO MW-1",
            "MAZE(I, 0) = 1",
            "NEXT",
            "WHILE DIST < 20000 AND HIT = 0",
            "IF MX < 0 OR MX >= MW OR MY < 0 OR MY >= MH THEN HIT = 1",
            "IF HIT = 0 THEN IF MAZE(MX, MY) = 1 THEN HIT = 1",
            "WEND",
            "IF HEIGHT > 200 THEN HEIGHT = 200",
            "ANGLE = ANGLE MOD 360",
            "IF ANGLE < 0 THEN ANGLE = ANGLE + 360",
            "IF MX >= 0 AND MX < MW AND MY >= 0 AND MY < MH THEN IF MAZE(MX, MY) = 1 THEN PLAYERX = PLAYERX - COSL(ANGLE) * SPEED / 100 : PLAYERY = PLAYERY - SINL(ANGLE) * SPEED / 100",
        };

        foreach (var pattern in patterns)
        {
            _output.WriteLine($"Testing pattern: {pattern}");
            var interpreter = new QBasicInterpreter(qb);
            
            try
            {
                // Provide minimal context for patterns that need it
                var testCode = pattern;
                if (pattern.Contains("MAZE") && pattern.Contains("("))
                {
                    testCode = "DIM MAZE(20, 20) AS INTEGER\n" + testCode;
                }
                if (pattern.Contains("COSL"))
                {
                    testCode = "DIM COSL(360) AS INTEGER\nMW = 20\nMH = 20\nS = 1000\nSPEED = 50\nANGLE = 0\nPLAYERX = 0\nPLAYERY = 0\n" + testCode;
                }
                if (pattern.Contains("MW") || pattern.Contains("MH"))
                {
                    testCode = "MW = 20\nMH = 20\n" + testCode;
                }
                if (pattern == "NEXT")
                {
                    testCode = "FOR I = 0 TO 10\n" + testCode;
                }
                if (pattern == "WEND")
                {
                    testCode = "WHILE 1 = 0\n" + testCode;
                }

                // Auto-append closers if this pattern opens a block
                var labelStubs = GenerateLabelStubs(testCode);
                if (!string.IsNullOrWhiteSpace(labelStubs)) testCode += "\n" + labelStubs;
                var closers = GenerateAutoClosers(testCode);
                if (!string.IsNullOrWhiteSpace(closers)) testCode += "\n" + closers;

                using var cts = new CancellationTokenSource(1);
                interpreter.Run(testCode + "\nEND", cts.Token);
                _output.WriteLine("  ✓ Parsed successfully");
            }
            catch (OperationCanceledException)
            {
                _output.WriteLine("  ✓ Parsed successfully");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"  ✗ ERROR: {ex.Message}");
                throw new Exception($"Failed to parse pattern: {pattern}", ex);
            }
        }
    }

    
}
