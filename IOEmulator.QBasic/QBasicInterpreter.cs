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
                    if (tokens.Count > 1)
                    {
                        if (tokens[1].Equals("INKEY$", StringComparison.OrdinalIgnoreCase))
                        {
                            var s = qb.INKEY();
                            if (s == "\b") { qb.PRINT("\b"); qb.PRINT(" "); qb.PRINT("\b"); ip++; break; }
                            qb.PRINT(s); _didTextOutput = true; ip++; break;
                        }
                        if (tokens[1].Equals("LASTKEY$", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_lastInkey == "\b") { qb.PRINT("\b"); qb.PRINT(" "); qb.PRINT("\b"); ip++; break; }
                            qb.PRINT(_lastInkey); _didTextOutput = true; ip++; break;
                        }
                        // PRINT variable or number
                        if (IsIdentifier(tokens[1]))
                        {
                            var name = tokens[1];
                            if (name.EndsWith("$")) { qb.PRINT(GetStr(name)); _didTextOutput = true; ip++; break; }
                            else { qb.PRINT(GetInt(name).ToString(CultureInfo.InvariantCulture)); _didTextOutput = true; ip++; break; }
                        }
                    }
                    qb.PRINT(ParseStringOrRemainder(tokens, 1)); _didTextOutput = true; ip++; break;
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
        // 1) IF INKEY$ <> "" THEN [GOTO label|END|PRINT INKEY$]
        // 2) IF <intExpr> {=|<|>} <intExpr> THEN [GOTO label|END|PRINT "text"]
        int i = 1;
        if (tokens[i].Equals("INKEY$", StringComparison.OrdinalIgnoreCase))
        {
            i++;
            var op1 = Expect(tokens, i++, "<>");
            var rhs1 = Expect(tokens, i++, ExpectStringLiteral: true);
            var thenTok1 = Expect(tokens, i++, "THEN");
            _lastInkey = qb.INKEY();
            bool cond1 = (_lastInkey != "");
            return DoThen(tokens, program, currentIp, i, cond1);
        }
        // Numeric compare
        int left = ParseIntExpr(tokens, i); // may read 1-4 tokens depending on expr
        // Advance i over left expr tokens: we only supported simple forms so skip until operator
        while (i < tokens.Count && tokens[i] != "=" && tokens[i] != "<" && tokens[i] != ">") i++;
        if (i >= tokens.Count) return currentIp + 1;
        string op = tokens[i++];
        int right = ParseIntExpr(tokens, i);
        // move i to THEN
        while (i < tokens.Count && !tokens[i].Equals("THEN", StringComparison.OrdinalIgnoreCase)) i++;
        if (i < tokens.Count && tokens[i].Equals("THEN", StringComparison.OrdinalIgnoreCase)) i++;
        bool cond = op switch
        {
            "=" => left == right,
            "<" => left < right,
            ">" => left > right,
            _ => false
        };
        return DoThen(tokens, program, currentIp, i, cond);
    }

    private int DoThen(List<string> tokens, ProgramIR program, int currentIp, int i, bool cond)
    {
        if (!cond) return currentIp + 1;
        if (i >= tokens.Count) return currentIp + 1;
        // Support multiple THEN-actions separated by ':' e.g., PRINT ".." : GOTO 10
        while (i < tokens.Count)
        {
            // skip separators
            while (i < tokens.Count && tokens[i] == ":") i++;
            if (i >= tokens.Count) break;
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
            if (action == "PRINT")
            {
                if (i + 1 < tokens.Count && tokens[i + 1].StartsWith("\""))
                {
                    qb.PRINT(Unquote(tokens[i + 1])); _didTextOutput = true;
                    i += 2; // consume PRINT and the string literal
                    continue;
                }
                if (i + 1 < tokens.Count && tokens[i + 1].Equals("INKEY$", StringComparison.OrdinalIgnoreCase))
                {
                    var s = _lastInkey;
                    if (s == "\b") { qb.PRINT("\b"); qb.PRINT(" "); qb.PRINT("\b"); }
                    else qb.PRINT(s);
                    _didTextOutput = true;
                    i += 2;
                    continue;
                }
                // Unknown print form; bail out to avoid infinite loop
                return currentIp + 1;
            }
            // Unrecognized then-action; stop processing
            break;
        }
        return currentIp + 1;
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
            qb.SLEEP(seconds);
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
        // Accept: PSET (x, y), c   OR  PSET x, y, c
        int i = 1;
        if (tokens[i] == "(") i++;
        int x = ParseIntSkipRaw(tokens, ref i);
        int y = ParseIntSkip(tokens, ref i);
        if (i < tokens.Count && tokens[i] == ")") i++;
        if (i < tokens.Count && tokens[i] == ",") i++;
        int color = ParseIntSkip(tokens, ref i);
        qb.PSET(x, y, color);
    }

    private void DoLINE(List<string> tokens)
    {
        // Accept: LINE (x1, y1)-(x2, y2), c  OR LINE x1, y1, x2, y2[, c]
        int x1, y1, x2, y2;
        int? color = null;
        int i = 1;
        if (tokens[i] == "(")
        {
            i++;
            x1 = ParseIntSkipRaw(tokens, ref i);
            y1 = ParseIntSkip(tokens, ref i);
            if (tokens[i] == ")") i++;
            if (tokens[i] == "-") i++;
            if (tokens[i] == "(") i++;
            x2 = ParseIntSkipRaw(tokens, ref i);
            y2 = ParseIntSkip(tokens, ref i);
            if (tokens[i] == ")") i++;
            if (i < tokens.Count && tokens[i] == ",")
            {
                i++;
                color = ParseIntSkip(tokens, ref i);
            }
        }
        else
        {
            x1 = ParseIntSkipRaw(tokens, ref i);
            y1 = ParseIntSkip(tokens, ref i);
            x2 = ParseIntSkip(tokens, ref i);
            y2 = ParseIntSkip(tokens, ref i);
            if (i < tokens.Count && tokens[i] == ",")
            {
                i++;
                color = ParseIntSkip(tokens, ref i);
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
                    if (code[j] == '"') { j++; break; }
                    j++;
                }
                tokens.Add(code[i..j]);
                i = j;
                continue;
            }
            if (",()-:;".IndexOf(c) >= 0)
            {
                tokens.Add(c.ToString()); i++; continue;
            }
            // identifier or number or operator
            int k = i;
            while (k < code.Length && !char.IsWhiteSpace(code[k]) && ",()-:;".IndexOf(code[k]) < 0)
                k++;
            tokens.Add(code[i..k]);
            i = k;
        }
        // Merge "-" that is used as connector in (x1,y1)-(x2,y2) already handled; operators like <> may be separate, ensure we join if split
        for (int t = 0; t < tokens.Count - 1; t++)
        {
            if (tokens[t] == "<" && tokens[t + 1] == ">")
            {
                tokens[t] = "<>";
                tokens.RemoveAt(t + 1);
                break;
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
        if (name.EndsWith("$"))
        {
            var value = ParseStringExpr(tokens, start + 2);
            SetStr(name, value);
        }
        else
        {
            var value = ParseIntExpr(tokens, start + 2);
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

    private int ParseIntExpr(List<string> tokens, int index)
    {
        if (index >= tokens.Count) return 0;
        var t = tokens[index];
        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return n;
        if (IsIdentifier(t))
        {
            if (t.Equals("VAL", StringComparison.OrdinalIgnoreCase))
            {
                // Expect: VAL ( name$ )
                if (tokens[index + 1] != "(") throw new InvalidOperationException("Expected '(' after VAL");
                var name = tokens[index + 2];
                if (!name.EndsWith("$")) throw new InvalidOperationException("VAL expects string variable");
                if (tokens[index + 3] != ")") throw new InvalidOperationException("Expected ')' after VAL argument");
                var s = GetStr(name);
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
                return 0;
            }
            if (t.Equals("RND", StringComparison.OrdinalIgnoreCase))
            {
                // RND(n) returns 1..n
                if (tokens[index + 1] != "(") throw new InvalidOperationException("Expected '(' after RND");
                int nmax = ParseIntExpr(tokens, index + 2);
                // find matching ')': we only support simple arg
                // ignore tokens[index+3] == ")"
                if (nmax <= 0) nmax = 1;
                return 1 + _rng.Next(nmax);
            }
            return GetInt(t);
        }
        return 0;
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
            return s.Substring(1, s.Length - 2);
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
