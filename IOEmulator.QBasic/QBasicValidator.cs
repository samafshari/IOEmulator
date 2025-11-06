using System;
using System.Collections.Generic;
using System.Linq;

namespace Neat;

public static class QBasicValidator
{
    public static void Validate(string source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var lines = SplitLines(source);
        var program = Preprocess(lines);

        // Check for structural errors
        CheckStructures(program);
        CheckLabels(program);
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

    private static ProgramIR Preprocess(string[] lines)
    {
        var ir = new ProgramIR();
        for (int i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0) continue;
            if (trimmed.StartsWith("'")) continue;
            if (trimmed.StartsWith("REM ", StringComparison.OrdinalIgnoreCase)) continue;

            if (trimmed.StartsWith("DATA ", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Skip DATA for validation
            }

            string? label = null;
            string code = trimmed;
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

            foreach (var stmt in SplitStatements(code))
            {
                var line = new Line { Label = label, Code = stmt, Index = ir.Lines.Count };
                if (!string.IsNullOrEmpty(label))
                {
                    if (!ir.LabelToIndex.TryAdd(label!, line.Index))
                        throw new InvalidOperationException($"Duplicate label: {label}");
                }
                ir.Lines.Add(line);
                label = null;
            }
        }
        return ir;
    }

    private static IEnumerable<string> SplitStatements(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) yield break;
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
                        if (j + 1 < code.Length && code[j + 1] == '"')
                        {
                            j += 2;
                            continue;
                        }
                        j++;
                        break;
                    }
                    j++;
                }
                tokens.Add(code[i..j]);
                i = j;
                continue;
            }
            if (",()-:;=+*/<>%".IndexOf(c) >= 0)
            {
                tokens.Add(c.ToString()); i++; continue;
            }
            int k = i;
            while (k < code.Length && !char.IsWhiteSpace(code[k]) && ",()-:;=+*/<>%".IndexOf(code[k]) < 0)
                k++;
            tokens.Add(code[i..k]);
            i = k;
        }
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

    private static void CheckStructures(ProgramIR program)
    {
        var structureStack = new Stack<string>();

        for (int ip = 0; ip < program.Lines.Count; ip++)
        {
            var line = program.Lines[ip];
            if (string.IsNullOrWhiteSpace(line.Code)) continue;
            var tokens = Tokenize(line.Code);
            if (tokens.Count == 0) continue;
            var head = tokens[0].ToUpperInvariant();

            switch (head)
            {
                case "FOR":
                    if (tokens.Count < 2) throw new InvalidOperationException($"Invalid FOR at line {line.Index + 1}");
                    var varName = tokens[1];
                    structureStack.Push("FOR " + varName);
                    break;
                case "NEXT":
                    if (structureStack.Count == 0 || !structureStack.Peek().StartsWith("FOR")) throw new InvalidOperationException($"NEXT without matching FOR at line {line.Index + 1}");
                    var nextVar = tokens.Count > 1 ? tokens[1] : null;
                    var forVar = structureStack.Pop().Substring(4);
                    if (nextVar != null && !nextVar.Equals(forVar, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"NEXT {nextVar} does not match FOR {forVar} at line {line.Index + 1}");
                    break;
                case "WHILE":
                    structureStack.Push("WHILE");
                    break;
                case "WEND":
                    if (structureStack.Count == 0 || structureStack.Peek() != "WHILE") throw new InvalidOperationException($"WEND without matching WHILE at line {line.Index + 1}");
                    structureStack.Pop();
                    break;
                case "IF":
                    if (IsBlockIf(tokens)) structureStack.Push("IF");
                    break;
                case "END":
                    if (tokens.Count > 1 && tokens[1].Equals("IF", StringComparison.OrdinalIgnoreCase))
                    {
                        if (structureStack.Count == 0 || structureStack.Peek() != "IF") throw new InvalidOperationException($"END IF without matching IF at line {line.Index + 1}");
                        structureStack.Pop();
                    }
                    break;
                case "ENDIF":
                    if (structureStack.Count == 0 || structureStack.Peek() != "IF") throw new InvalidOperationException($"END IF without matching IF at line {line.Index + 1}");
                    structureStack.Pop();
                    break;
                case "DO":
                    structureStack.Push("DO");
                    break;
                case "LOOP":
                    if (structureStack.Count == 0 || structureStack.Peek() != "DO") throw new InvalidOperationException($"LOOP without matching DO at line {line.Index + 1}");
                    structureStack.Pop();
                    break;
            }
        }

        if (structureStack.Any(s => s.StartsWith("FOR"))) throw new InvalidOperationException($"Unclosed FOR loop");
        if (structureStack.Any(s => s == "WHILE")) throw new InvalidOperationException($"Unclosed WHILE loop");
        if (structureStack.Any(s => s == "IF")) throw new InvalidOperationException($"Unclosed IF block");
        if (structureStack.Any(s => s == "DO")) throw new InvalidOperationException($"Unclosed DO loop");
    }

    private static bool IsBlockIf(List<string> tokens)
    {
        if (tokens.Count == 0) return false;
        if (!tokens[0].Equals("IF", StringComparison.OrdinalIgnoreCase)) return false;
        int thenPos = -1;
        for (int i = 1; i < tokens.Count; i++)
        {
            if (tokens[i].Equals("THEN", StringComparison.OrdinalIgnoreCase))
            {
                thenPos = i;
                break;
            }
        }
        return thenPos >= 0 && thenPos == tokens.Count - 1;
    }

    private static void CheckLabels(ProgramIR program)
    {
        for (int ip = 0; ip < program.Lines.Count; ip++)
        {
            var line = program.Lines[ip];
            if (string.IsNullOrWhiteSpace(line.Code)) continue;
            var tokens = Tokenize(line.Code);
            if (tokens.Count == 0) continue;
            if (tokens[0].Equals("GOTO", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Count < 2) throw new InvalidOperationException($"Invalid GOTO at line {line.Index + 1}");
                var label = tokens[1];
                // If target looks like a numeric line label, defer to runtime instead of static validation
                if (int.TryParse(label, out _)) continue;
                if (!program.LabelToIndex.ContainsKey(label))
                    throw new InvalidOperationException($"Undefined label '{label}' at line {line.Index + 1}");
            }
        }
    }
}