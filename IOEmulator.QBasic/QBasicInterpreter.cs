using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Neat;

// A minimal QBASIC-like interpreter executing against QBasicApi
// Supported statements:
// - SCREEN n
// - COLOR f[, b]
// - CLS
// - LOCATE r, c
// - PRINT "text"
// - PSET (x, y), color  | also supports PSET x, y, color
// - LINE (x1, y1)-(x2, y2), color | also supports LINE x1, y1, x2, y2[, color]
// - BEEP
// - SOUND f, d
// - SLEEP [seconds]
// - GOTO label
// - label: (definition)
// - IF INKEY$ <> "" THEN [GOTO label|END]
// - END
public class QBasicInterpreter
{
    private readonly QBasicApi qb;
    private string _lastInkey = string.Empty;
    private readonly Dictionary<string, int> _ints = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _strs = new(StringComparer.OrdinalIgnoreCase);
    private Random _rng = new Random();
    private bool _didTextOutput = false;

    /// <summary>
    /// Speed factor for execution and delays. 
    /// 1.0 = normal speed, 2.0 = double speed (half delays), 0.5 = half speed (double delays).
    /// Affects SLEEP and internal iteration throttling for fast tests.
    /// </summary>
    public double SpeedFactor { get; set; } = 1.0;

    public QBasicInterpreter(QBasicApi api)
    {
        qb = api ?? throw new ArgumentNullException(nameof(api));
    }

    public void Run(string source, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        try
        {
            var lines = SplitLines(source);
            var program = Preprocess(lines);
            Execute(program, cancellationToken);
            // On normal termination (no exception): show an end message like DOS QBASIC
            try
            {
                if (_didTextOutput)
                {
                    // Use a visible color and print at the bottom row to avoid covering user graphics
                    qb.COLOR(15, qb.Emulator.BackgroundColorIndex);
                    qb.LOCATE(qb.Emulator.TextRows - 1, 0);
                    qb.PRINT("Press any key to continue . . .\r\n");
                }
            }
            catch { /* ignore secondary failures while reporting */ }
        }
        catch (OperationCanceledException)
        {
            // Treat cancellation as a normal termination path
        }
        catch (Exception ex)
        {
            // Graceful error reporting similar to QBASIC: print an error message and terminate
            try
            {
                // Ensure readable colors for the error line
                qb.COLOR(15, qb.Emulator.BackgroundColorIndex);
                qb.PRINT($"Error: {ex.Message}");
                qb.PRINT("\r\n");
            }
            catch { /* ignore secondary failures while reporting */ }
        }
    }

    private static string[] SplitLines(string src)
        => src.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    private sealed class Line
    {
        public string? Label;
        public string Code = string.Empty;
        public int Index;
    }

    private sealed class ProgramIR
    {
        public List<Line> Lines = new();
        public Dictionary<string, int> LabelToIndex = new(StringComparer.OrdinalIgnoreCase);
    }

    private ProgramIR Preprocess(string[] lines)
    {
        var ir = new ProgramIR();
        for (int i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            // Strip comments starting with ' or REM (at start)
            var trimmed = raw.Trim();
            if (trimmed.Length == 0) continue;
            if (trimmed.StartsWith("'")) continue;
            if (trimmed.StartsWith("REM ", StringComparison.OrdinalIgnoreCase)) continue;

            string? label = null;
            string code = trimmed;
            // Detect leading label strictly at start: [A-Za-z0-9_]+:
            int j = 0;
            while (j < trimmed.Length && (char.IsLetterOrDigit(trimmed[j]) || trimmed[j] == '_')) j++;
            if (j > 0 && j < trimmed.Length && trimmed[j] == ':')
            {
                label = trimmed[..j].Trim();
                code = trimmed[(j + 1)..].Trim();
            }
            else if (trimmed.EndsWith(":"))
            {
                label = trimmed[..^1].Trim();
                code = string.Empty;
            }

            // If there's a label with no code, still register an empty line to mark the label position
            if (string.IsNullOrWhiteSpace(code))
            {
                if (!string.IsNullOrEmpty(label))
                {
                    var line = new Line { Label = label, Code = string.Empty, Index = ir.Lines.Count };
                    if (!ir.LabelToIndex.TryAdd(label!, line.Index))
                        throw new InvalidOperationException($"Duplicate label: {label}");
                    ir.Lines.Add(line);
                }
                continue;
            }

            // Split the remaining code into multiple statements separated by top-level ':'
            foreach (var stmt in SplitStatements(code))
            {
                var line = new Line { Label = label, Code = stmt, Index = ir.Lines.Count };
                if (!string.IsNullOrEmpty(label))
                {
                    if (!ir.LabelToIndex.TryAdd(label!, line.Index))
                        throw new InvalidOperationException($"Duplicate label: {label}");
                }
                ir.Lines.Add(line);
                // Only the first statement carries the label
                label = null;
            }
        }
        return ir;
    }

    private static int IndexOfTopLevelColon(string s)
    {
        // Find colon used to separate label:statement at the start; ignore colons inside strings
        bool inStr = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '"') inStr = !inStr;
            else if (c == ':' && !inStr) return i;
        }
        return -1;
    }

    private static IEnumerable<string> SplitStatements(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) yield break;
        // Do not split IF ... THEN action lists; they remain a single statement so THEN-actions keep their ':'
        if (code.TrimStart().StartsWith("IF ", StringComparison.OrdinalIgnoreCase))
        {
            yield return code.Trim();
            yield break;
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

    private void Execute(ProgramIR program, CancellationToken ct)
    {
        int ip = 0;
        while (ip < program.Lines.Count)
        {
            ct.ThrowIfCancellationRequested();
            var line = program.Lines[ip];
            if (string.IsNullOrWhiteSpace(line.Code))
            {
                ip++;
                continue;
            }
            var tokens = Tokenize(line.Code);
            if (tokens.Count == 0) { ip++; continue; }
            var head = tokens[0].ToUpperInvariant();
            switch (head)
            {
                case "RANDOMIZE":
                    _rng = new Random(); ip++; break;
                case "SCREEN":
                    qb.SCREEN(ParseInt(tokens, 1));
                    ip++;
                    break;
                case "COLOR":
                    DoCOLOR(tokens);
                    ip++;
                    break;
                case "CLS":
                    qb.CLS(); ip++; break;
                case "LOCATE":
                    {
                        int iLoc = 1;
                        int r = ParseIntSkip(tokens, ref iLoc);
                        int c = ParseIntSkip(tokens, ref iLoc);
                        qb.LOCATE(r, c); ip++; break;
                    }
                case "PRINT":
                    {
                        // QBASIC semantics: PRINT appends a newline unless a trailing ';' is present.
                        bool suppressNewline = tokens.Count > 1 && tokens[^1] == ";";
                        if (tokens.Count == 1)
                        {
                            // bare PRINT -> just a newline
                            qb.PRINT("\r\n"); _didTextOutput = true; ip++; break;
                        }
                        if (tokens[1].Equals("INKEY$", StringComparison.OrdinalIgnoreCase))
                        {
                            var s = qb.INKEY();
                            if (s == "\b") { qb.PRINT("\b"); qb.PRINT(" "); qb.PRINT("\b"); _didTextOutput = true; ip++; break; }
                            qb.PRINT(s); _didTextOutput = true; if (!suppressNewline) qb.PRINT("\r\n"); ip++; break;
                        }
                        if (tokens[1].Equals("LASTKEY$", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_lastInkey == "\b") { qb.PRINT("\b"); qb.PRINT(" "); qb.PRINT("\b"); _didTextOutput = true; ip++; break; }
                            qb.PRINT(_lastInkey); _didTextOutput = true; if (!suppressNewline) qb.PRINT("\r\n"); ip++; break;
                        }
                        // PRINT variable or number
                        if (IsIdentifier(tokens[1]))
                        {
                            var name = tokens[1];
                            if (name.EndsWith("$")) { qb.PRINT(GetStr(name)); _didTextOutput = true; if (!suppressNewline) qb.PRINT("\r\n"); ip++; break; }
                            else { qb.PRINT(GetInt(name).ToString(CultureInfo.InvariantCulture)); _didTextOutput = true; if (!suppressNewline) qb.PRINT("\r\n"); ip++; break; }
                        }
                        // Default: print remainder as string; if suppressing newline, drop trailing ';' from output
                        if (suppressNewline)
                        {
                            var content = ParseStringOrRemainder(tokens.Take(tokens.Count - 1).ToList(), 1);
                            qb.PRINT(content); _didTextOutput = true; ip++; break;
                        }
                        else
                        {
                            qb.PRINT(ParseStringOrRemainder(tokens, 1)); _didTextOutput = true; qb.PRINT("\r\n"); ip++; break;
                        }
                    }
                case "LET":
                    DoAssignment(tokens, 1); ip++; break;
                case "PSET":
                    DoPSET(tokens); ip++; break;
                case "LINE":
                    if (tokens.Count > 1 && tokens[1].Equals("INPUT", StringComparison.OrdinalIgnoreCase))
                    {
                        string prompt = string.Empty;
                        string? targetVar = null;
                        // Syntax: LINE INPUT "prompt" ; var$
                        // tokens: [LINE][INPUT]["..."][;][VAR$]
                        int ti = 2;
                        if (ti < tokens.Count && tokens[ti].StartsWith("\""))
                        {
                            prompt = Unquote(tokens[ti]); ti++;
                        }
                        if (ti < tokens.Count && tokens[ti] == ";") { ti++; }
                        if (ti < tokens.Count && IsIdentifier(tokens[ti])) targetVar = tokens[ti];
                        _didTextOutput = true;
                        var s = qb.LineInput(prompt, ct);
                        if (!string.IsNullOrEmpty(targetVar)) SetStr(targetVar!, s);
                        else if (!string.IsNullOrEmpty(s)) { qb.PRINT(s); _didTextOutput = true; }
                        ip++;
                        break;
                    }
                    DoLINE(tokens); ip++; break;
                case "BEEP":
                    qb.BEEP(); ip++; break;
                case "SOUND":
                    DoSOUND(tokens); ip++; break;
                case "PLAY":
                    {
                        // PLAY <stringExpr>
                        var music = ParseStringExpr(tokens, 1);
                        qb.PLAY(music);
                        ip++;
                        break;
                    }
                case "SLEEP":
                    DoSLEEP(tokens, ct); ip++; break;
                case "IF":
                    ip = DoIF(tokens, program, ip); break;
                case "GOTO":
                    ip = ResolveLabel(program, ExpectIdentifier(tokens, 1)); break;
                case "END":
                    return;
                default:
                    // Support assignment without LET: name = expr
                    if (IsIdentifier(tokens[0]) && tokens.Count > 2 && tokens[1] == "=")
                    {
                        DoAssignment(tokens, 0); ip++; break;
                    }
                    throw new InvalidOperationException($"Unknown statement: {head}");
            }
        }
    }

    private int DoIF(List<string> tokens, ProgramIR program, int currentIp)
    {
        // Two forms supported:
        // 1) IF INKEY$ <> "" THEN actions [ELSE actions]
        // 2) IF <intExpr> {=|<|>|<=|>=|<>} <intExpr> THEN actions [ELSE actions]
        int i = 1;
        if (tokens[i].Equals("INKEY$", StringComparison.OrdinalIgnoreCase))
        {
            i++;
            var op1 = Expect(tokens, i++, "<>");
            var rhs1 = Expect(tokens, i++, ExpectStringLiteral: true);
            var thenTok1 = Expect(tokens, i++, "THEN");
            _lastInkey = qb.INKEY();
            bool cond1 = (_lastInkey != "");
            int elseIndex = IndexOfToken(tokens, "ELSE", i);
            if (cond1)
            {
                return ExecuteActionList(tokens, program, currentIp, i, elseIndex);
            }
            else
            {
                if (elseIndex >= 0) return ExecuteActionList(tokens, program, currentIp, elseIndex + 1, -1);
                return currentIp + 1;
            }
        }
    // Numeric compare
    int left = ParseIntExprAdv(tokens, ref i);
        if (i >= tokens.Count) return currentIp + 1;
        string op = tokens[i++];
    int right = ParseIntExprAdv(tokens, ref i);
        // move i to THEN
        while (i < tokens.Count && !tokens[i].Equals("THEN", StringComparison.OrdinalIgnoreCase)) i++;
        if (i < tokens.Count && tokens[i].Equals("THEN", StringComparison.OrdinalIgnoreCase)) i++;
        bool cond = op switch
        {
            "=" => left == right,
            "<" => left < right,
            ">" => left > right,
            "<=" => left <= right,
            ">=" => left >= right,
            "<>" => left != right,
            _ => false
        };
        int elsePos = IndexOfToken(tokens, "ELSE", i);
        if (cond)
        {
            return ExecuteActionList(tokens, program, currentIp, i, elsePos);
        }
        else
        {
            if (elsePos >= 0) return ExecuteActionList(tokens, program, currentIp, elsePos + 1, -1);
            return currentIp + 1;
        }
    }

    private static int IndexOfToken(List<string> tokens, string keyword, int start)
    {
        for (int k = start; k < tokens.Count; k++)
        {
            if (tokens[k].Equals(keyword, StringComparison.OrdinalIgnoreCase)) return k;
        }
        return -1;
    }

    private int DoThen(List<string> tokens, ProgramIR program, int currentIp, int i, bool cond)
        => cond ? ExecuteActionList(tokens, program, currentIp, i, -1) : currentIp + 1;

    private int ExecuteActionList(List<string> tokens, ProgramIR program, int currentIp, int startIndex, int endIndex)
    {
        int i = startIndex;
        int stop = endIndex >= 0 ? endIndex : tokens.Count;
        while (i < stop)
        {
            // skip separators
            while (i < stop && tokens[i] == ":") i++;
            if (i >= stop) break;
            var action = tokens[i].ToUpperInvariant();
            if (action == "GOTO")
            {
                var label = ExpectIdentifier(tokens, i + 1);
                return ResolveLabel(program, label);
            }
            if (action == "END")
            {
                return program.Lines.Count;
            }
            if (action == "IF")
            {
                int localStop = FindNextSeparator(tokens, i, stop);
                int jump = ExecuteInlineIF(tokens, program, currentIp, i, localStop);
                if (jump >= 0) return jump;
                i = localStop; // advance to next statement separator/end
                continue;
            }
            if (action == "PRINT")
            {
                if (i + 1 < stop && tokens[i + 1].StartsWith("\""))
                {
                    qb.PRINT(Unquote(tokens[i + 1])); _didTextOutput = true; qb.PRINT("\r\n");
                    i += 2; continue;
                }
                if (i + 1 < stop && tokens[i + 1].Equals("INKEY$", StringComparison.OrdinalIgnoreCase))
                {
                    var s = _lastInkey;
                    if (s == "\b") { qb.PRINT("\b"); qb.PRINT(" "); qb.PRINT("\b"); }
                    else { qb.PRINT(s); qb.PRINT("\r\n"); }
                    _didTextOutput = true; i += 2; continue;
                }
                // Unknown print form; stop processing list to avoid misparse
                return currentIp + 1;
            }
            if (action == "PSET")
            {
                var sub = tokens.GetRange(i, stop - i);
                DoPSET(sub);
                while (i < stop && tokens[i] != ":") i++;
                continue;
            }
            // Assignment inside THEN/ELSE: name = expr
            if (IsIdentifier(tokens[i]) && i + 1 < stop && tokens[i + 1] == "=")
            {
                var sub = tokens.GetRange(i, stop - i);
                DoAssignment(sub, 0);
                while (i < stop && tokens[i] != ":") i++;
                continue;
            }
            break;
        }
        return currentIp + 1;
    }

    private static int FindNextSeparator(List<string> tokens, int start, int stop)
    {
        for (int k = start; k < stop; k++)
        {
            if (tokens[k] == ":") return k;
        }
        return stop;
    }

    // Execute a nested IF inside a statement list; returns jump ip if a control transfer occurs, else -1
    private int ExecuteInlineIF(List<string> tokens, ProgramIR program, int currentIp, int start, int stop)
    {
        int i = start + 1; // skip IF
        // Support same forms as top-level
        if (i < stop && tokens[i].Equals("INKEY$", StringComparison.OrdinalIgnoreCase))
        {
            i++;
            var op1 = Expect(tokens, i++, "<>");
            var rhs1 = Expect(tokens, i++, ExpectStringLiteral: true);
            if (i < stop && tokens[i].Equals("THEN", StringComparison.OrdinalIgnoreCase)) i++;
            _lastInkey = qb.INKEY();
            bool cond1 = (_lastInkey != "");
            int elsePos = IndexOfTokenBounded(tokens, "ELSE", i, stop);
            if (cond1)
            {
                return ExecuteActionList(tokens, program, currentIp, i, elsePos);
            }
            else
            {
                if (elsePos >= 0) return ExecuteActionList(tokens, program, currentIp, elsePos + 1, stop);
                return -1;
            }
        }
        int left = ParseIntExprAdv(tokens, ref i);
        if (i >= stop) return -1;
        string op = tokens[i++];
        int right = ParseIntExprAdv(tokens, ref i);
        while (i < stop && !tokens[i].Equals("THEN", StringComparison.OrdinalIgnoreCase)) i++;
        if (i < stop && tokens[i].Equals("THEN", StringComparison.OrdinalIgnoreCase)) i++;
        bool cond = op switch
        {
            "=" => left == right,
            "<" => left < right,
            ">" => left > right,
            "<=" => left <= right,
            ">=" => left >= right,
            "<>" => left != right,
            _ => false
        };
        int elsePos2 = IndexOfTokenBounded(tokens, "ELSE", i, stop);
        if (cond)
        {
            return ExecuteActionList(tokens, program, currentIp, i, elsePos2);
        }
        else
        {
            if (elsePos2 >= 0) return ExecuteActionList(tokens, program, currentIp, elsePos2 + 1, stop);
            return -1;
        }
    }

    private static int IndexOfTokenBounded(List<string> tokens, string keyword, int start, int stop)
    {
        for (int k = start; k < stop; k++)
        {
            if (tokens[k].Equals(keyword, StringComparison.OrdinalIgnoreCase)) return k;
        }
        return -1;
    }

    private int ResolveLabel(ProgramIR program, string label)
    {
        if (!program.LabelToIndex.TryGetValue(label, out var idx))
            throw new InvalidOperationException($"Undefined label: {label}");
        return idx;
    }

    private void DoSLEEP(List<string> tokens, CancellationToken ct)
    {
        if (tokens.Count == 1)
        {
            // Block on emulator key wait directly to avoid any scheduling races
            qb.Emulator.WaitForKey(ct);
        }
        else
        {
            var seconds = ParseInt(tokens, 1);
            if (seconds <= 0) return; // Non-blocking for zero or negative
            // Apply speed factor to sleep duration
            int adjustedMs = (int)(seconds * 1000 / SpeedFactor);
            if (adjustedMs > 0)
            {
                using var cts = new CancellationTokenSource(adjustedMs);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
                try { qb.Emulator.WaitForKey(linked.Token); }
                catch (OperationCanceledException) { /* timeout or external cancel */ }
            }
        }
    }

    private void DoCOLOR(List<string> tokens)
    {
        // COLOR f[, b]
        int i = 1;
        int fg = ParseIntSkip(tokens, ref i);
        int? bg = null;
        if (i < tokens.Count)
        {
            bg = ParseIntSkip(tokens, ref i);
        }
        qb.COLOR(fg, bg);
    }

    private void DoPSET(List<string> tokens)
    {
        // Accept: PSET (x, y), c   OR  PSET x, y, c   where x,y,c are int expressions or identifiers
        int i = 1;
        if (i < tokens.Count && tokens[i] == "(") i++;
        int x = ParseIntExprAdv(tokens, ref i);
        if (i < tokens.Count && tokens[i] == ",") i++;
        int y = ParseIntExprAdv(tokens, ref i);
        if (i < tokens.Count && tokens[i] == ")") i++;
        if (i < tokens.Count && tokens[i] == ",") i++;
        int color = ParseIntExprAdv(tokens, ref i);
        qb.PSET(x, y, color);
    }

    private void DoLINE(List<string> tokens)
    {
        // Accept: LINE (x1, y1)-(x2, y2), c  OR LINE x1, y1, x2, y2[, c] with expressions
        int x1, y1, x2, y2;
        int? color = null;
        int i = 1;
        if (i < tokens.Count && tokens[i] == "(")
        {
            i++;
            x1 = ParseIntExprAdv(tokens, ref i);
            if (i < tokens.Count && tokens[i] == ",") i++;
            y1 = ParseIntExprAdv(tokens, ref i);
            if (i < tokens.Count && tokens[i] == ")") i++;
            if (i < tokens.Count && tokens[i] == "-") i++;
            if (i < tokens.Count && tokens[i] == "(") i++;
            x2 = ParseIntExprAdv(tokens, ref i);
            if (i < tokens.Count && tokens[i] == ",") i++;
            y2 = ParseIntExprAdv(tokens, ref i);
            if (i < tokens.Count && tokens[i] == ")") i++;
            if (i < tokens.Count && tokens[i] == ",")
            {
                i++;
                color = ParseIntExprAdv(tokens, ref i);
            }
        }
        else
        {
            x1 = ParseIntExprAdv(tokens, ref i);
            if (i < tokens.Count && tokens[i] == ",") i++;
            y1 = ParseIntExprAdv(tokens, ref i);
            if (i < tokens.Count && tokens[i] == ",") i++;
            x2 = ParseIntExprAdv(tokens, ref i);
            if (i < tokens.Count && tokens[i] == ",") i++;
            y2 = ParseIntExprAdv(tokens, ref i);
            if (i < tokens.Count && tokens[i] == ",")
            {
                i++;
                color = ParseIntExprAdv(tokens, ref i);
            }
        }
        qb.LINE(x1, y1, x2, y2, color);
    }

    private static List<string> Tokenize(string code)
    {
        var tokens = new List<string>();
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
                    if (code[j] == '"')
                    {
                        // QBASIC-style escape for quotes: doubled quotes within a string literal
                        if (j + 1 < code.Length && code[j + 1] == '"')
                        {
                            j += 2; // consume the doubled quote and continue inside the string
                            continue;
                        }
                        j++; // closing quote
                        break;
                    }
                    j++;
                }
                tokens.Add(code[i..j]);
                i = j;
                continue;
            }
            if (",()-:;=+*/<>".IndexOf(c) >= 0)
            {
                tokens.Add(c.ToString()); i++; continue;
            }
            // identifier or number
            int k = i;
            while (k < code.Length && !char.IsWhiteSpace(code[k]) && ",()-:;=+*/<>".IndexOf(code[k]) < 0)
                k++;
            tokens.Add(code[i..k]);
            i = k;
        }
        // Merge two-char operators: <>, <=, >=
        for (int t = 0; t < tokens.Count - 1; t++)
        {
            if (tokens[t] == "<" && tokens[t + 1] == ">")
            {
                tokens[t] = "<>";
                tokens.RemoveAt(t + 1);
                t--;
                continue;
            }
            if (tokens[t] == "<" && tokens[t + 1] == "=")
            {
                tokens[t] = "<=";
                tokens.RemoveAt(t + 1);
                t--;
                continue;
            }
            if (tokens[t] == ">" && tokens[t + 1] == "=")
            {
                tokens[t] = ">=";
                tokens.RemoveAt(t + 1);
                t--;
                continue;
            }
        }
        return tokens;
    }

    private bool IsIdentifier(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (!(char.IsLetter(s[0]) || s[0] == '_')) return false;
        return true;
    }

    private void DoAssignment(List<string> tokens, int start)
    {
        // name = expr
        var name = tokens[start];
        if (tokens[start + 1] != "=") throw new InvalidOperationException("Expected '=' in assignment");
        int idx = start + 2;
        if (name.EndsWith("$"))
        {
            var value = ParseStringExpr(tokens, idx);
            SetStr(name, value);
        }
        else
        {
            var value = ParseIntExprAdv(tokens, ref idx);
            SetInt(name, value);
        }
    }

    private string ParseStringExpr(List<string> tokens, int index)
    {
        if (index >= tokens.Count) return string.Empty;
        var t = tokens[index];
        if (t.Length > 0 && t[0] == '"') return Unquote(t);
        if (IsIdentifier(t)) return GetStr(t);
        return string.Empty;
    }

    private int ParseIntExprAdv(List<string> tokens, ref int index)
    {
        // Recursive-descent with precedence: factor (*,/), term (+,-)
        int ParseFactor(ref int i)
        {
            if (i >= tokens.Count) return 0;
            var tok = tokens[i];
            if (tok == "+") { i++; return ParseFactor(ref i); }
            if (tok == "-") { i++; return -ParseFactor(ref i); }
            if (tok == "(") { i++; var val = ParseExpr(ref i); if (i < tokens.Count && tokens[i] == ")") i++; return val; }
            // number
            if (int.TryParse(tok, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) { i++; return n; }
            // identifier or function
            if (IsIdentifier(tok))
            {
                if (tok.Equals("VAL", StringComparison.OrdinalIgnoreCase))
                {
                    i++; if (tokens[i] != "(") throw new InvalidOperationException("Expected '(' after VAL");
                    i++; var name = tokens[i++]; if (!name.EndsWith("$")) throw new InvalidOperationException("VAL expects string variable");
                    if (tokens[i] != ")") throw new InvalidOperationException("Expected ')' after VAL argument"); i++;
                    var s = GetStr(name);
                    return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
                }
                if (tok.Equals("RND", StringComparison.OrdinalIgnoreCase))
                {
                    i++; if (tokens[i] != "(") throw new InvalidOperationException("Expected '(' after RND");
                    i++; int nmax = ParseTerm(ref i); if (tokens[i] != ")") throw new InvalidOperationException("Expected ')' after RND"); i++;
                    if (nmax <= 0) nmax = 1; return 1 + _rng.Next(nmax);
                }
                if (tok.Equals("PX", StringComparison.OrdinalIgnoreCase))
                {
                    i++; if (tokens[i] != "(") throw new InvalidOperationException("Expected '(' after PX");
                    i++; int x = ParseExpr(ref i); if (tokens[i] != ",") throw new InvalidOperationException("Expected ',' in PX");
                    i++; int y = ParseExpr(ref i); if (tokens[i] != ")") throw new InvalidOperationException("Expected ')' after PX"); i++;
                    // Return 0 if background, 1 otherwise (also out-of-bounds treated as 1)
                    if (x < 0 || y < 0 || x >= qb.Emulator.ResolutionW || y >= qb.Emulator.ResolutionH) return 1;
                    var rgb = qb.POINT(x, y);
                    var bg = qb.Emulator.GetColor(qb.Emulator.BackgroundColorIndex);
                    return (rgb.R == bg.R && rgb.G == bg.G && rgb.B == bg.B) ? 0 : 1;
                }
                if (tok.Equals("PC", StringComparison.OrdinalIgnoreCase))
                {
                    i++; if (tokens[i] != "(") throw new InvalidOperationException("Expected '(' after PC");
                    i++; int x = ParseExpr(ref i); if (tokens[i] != ",") throw new InvalidOperationException("Expected ',' in PC");
                    i++; int y = ParseExpr(ref i); if (tokens[i] != ")") throw new InvalidOperationException("Expected ')' after PC"); i++;
                    if (x < 0 || y < 0 || x >= qb.Emulator.ResolutionW || y >= qb.Emulator.ResolutionH) return 255;
                    var rgb = qb.POINT(x, y);
                    // Find palette index match (exact)
                    for (int pi = 0; pi < qb.Emulator.Palette.Length; pi++)
                    {
                        var c = qb.Emulator.GetColor(pi);
                        if (c.R == rgb.R && c.G == rgb.G && c.B == rgb.B) return pi;
                    }
                    return 255;
                }
                // variable
                i++; return GetInt(tok);
            }
            // unknown
            i++; return 0;
        }

        int ParseTerm(ref int i)
        {
            int val = ParseFactor(ref i);
            while (i < tokens.Count && (tokens[i] == "*" || tokens[i] == "/"))
            {
                string op = tokens[i++];
                int rhs = ParseFactor(ref i);
                val = op == "*" ? val * rhs : (rhs == 0 ? 0 : val / rhs);
            }
            return val;
        }

        int ParseExpr(ref int i)
        {
            int val = ParseTerm(ref i);
            while (i < tokens.Count && (tokens[i] == "+" || tokens[i] == "-"))
            {
                string op = tokens[i++];
                int rhs = ParseTerm(ref i);
                val = op == "+" ? val + rhs : val - rhs;
            }
            return val;
        }

        return ParseExpr(ref index);
    }

    private void SetInt(string name, int value) => _ints[name] = value;
    private int GetInt(string name) => _ints.TryGetValue(name, out var v) ? v : 0;
    private void SetStr(string name, string value) => _strs[name] = value ?? string.Empty;
    private string GetStr(string name) => _strs.TryGetValue(name, out var v) ? v : string.Empty;

    private void DoSOUND(List<string> tokens)
    {
        int i = 1;
        int f = ParseIntSkip(tokens, ref i);
        int d = ParseIntSkip(tokens, ref i);
        qb.SOUND(f, d);
    }

    private static int ParseInt(List<string> tokens, int index)
        => int.Parse(tokens[index], CultureInfo.InvariantCulture);

    private static int ParseIntSkip(List<string> tokens, ref int index)
    {
        while (index < tokens.Count && (tokens[index] == "," || tokens[index] == ")" || tokens[index] == "(")) index++;
        return int.Parse(tokens[index++], CultureInfo.InvariantCulture);
    }

    private static int ParseIntSkipRaw(List<string> tokens, ref int index)
        => int.Parse(tokens[index++], CultureInfo.InvariantCulture);

    private static string ParseStringOrRemainder(List<string> tokens, int index)
    {
        if (index >= tokens.Count) return string.Empty;
        var tok = tokens[index];
        if (tok.Length > 0 && tok[0] == '"') return Unquote(tok);
        // Remainder of tokens re-joined with spaces for convenience
        return string.Join(" ", tokens.Skip(index));
    }

    private static string Unquote(string s)
    {
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
        {
            var inner = s.Substring(1, s.Length - 2);
            // Replace QBASIC doubled quotes with a single quote character
            inner = inner.Replace("\"\"", "\"");
            return inner;
        }
        return s;
    }

    private static string ExpectIdentifier(List<string> tokens, int index)
    {
        var id = tokens[index];
        return id.TrimEnd(':');
    }

    private static string Expect(List<string> tokens, int index, string? exact = null, bool ExpectStringLiteral = false)
    {
        if (index >= tokens.Count) throw new InvalidOperationException("Unexpected end of line");
        var t = tokens[index];
        if (exact != null && !t.Equals(exact, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Expected '{exact}' but got '{t}'");
        if (ExpectStringLiteral && !(t.StartsWith("\"") && t.EndsWith("\"")))
            throw new InvalidOperationException("Expected string literal");
        return t;
    }
}
