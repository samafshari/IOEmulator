using System;
using System.Text;
using System.Threading;

namespace Neat;

public sealed class LineEditor
{
    private readonly IOEmulator _io;
    private readonly QBasicScheduler _sched;

    public sealed class Options
    {
        public bool Blink = true;
        public int BlinkMs = 400;
        public int MaxLength = 255;
    }

    public LineEditor(IOEmulator io, QBasicScheduler sched)
    {
        _io = io ?? throw new ArgumentNullException(nameof(io));
        _sched = sched ?? throw new ArgumentNullException(nameof(sched));
    }

    public string ReadLine(string prompt, CancellationToken ct, Options? options = null)
    {
        options ??= new Options();
        // Render prompt at current cursor position
        if (!string.IsNullOrEmpty(prompt)) _io.PutString(prompt);
        int row = _io.CursorY;
        int startCol = _io.CursorX;
        int maxCols = _io.TextCols > 0 ? _io.TextCols : 80;
        int editableCols = Math.Max(0, maxCols - startCol);
        int limit = Math.Min(editableCols, options.MaxLength);

    var buf = new StringBuilder();
        int caret = 0; // index into buf
        bool caretOn = false;
        long lastBlink = Environment.TickCount64;
    int lastCleared = 0; // number of cells last cleared (for orphan caret cleanup)

        void Render(bool withCaret)
        {
            // Draw buffer text
            // Clear the editable region by overwriting with spaces. Clear up to the max of
            // previous visible length and current visible length to erase any orphan caret/underscore.
            int visibleLen = Math.Min(buf.Length + 1, editableCols);
            int clearLen = Math.Max(lastCleared, visibleLen);
            for (int i = 0; i < clearLen; i++)
            {
                _io.WriteTextAt(startCol + i, row, ' ');
            }
            // Draw the characters
            for (int i = 0; i < buf.Length && i < editableCols; i++)
            {
                _io.WriteTextAt(startCol + i, row, buf[i]);
            }
            if (withCaret && options.Blink)
            {
                // Caret at current position; if at end, draw underscore; otherwise invert char by swapping colors
                int col = startCol + Math.Min(caret, editableCols - 1);
                int fg = _io.ForegroundColorIndex;
                int bg = _io.BackgroundColorIndex;
                if (caret < buf.Length)
                {
                    _io.WriteTextAt(col, row, buf[caret], bg, fg); // inverted colors on caret cell
                }
                else
                {
                    _io.WriteTextAt(col, row, '_', fg, bg);
                }
            }
            lastCleared = visibleLen;
        }

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            // Blink handling
            long now = Environment.TickCount64;
            if (options.Blink && (now - lastBlink) >= options.BlinkMs)
            {
                caretOn = !caretOn;
                lastBlink = now;
                Render(caretOn);
            }

            // Poll for key without blocking UI too long
            if (_io.TryReadKey(out var ev))
            {
                if (ev.Type != KeyEventType.Down) continue;
                // Reset caret state on any key activity
                caretOn = true; lastBlink = now;

                if (ev.Char.HasValue && !char.IsControl(ev.Char.Value))
                {
                    if (buf.Length < limit)
                    {
                        // Insert at caret
                        buf.Insert(caret, ev.Char.Value);
                        caret++;
                    }
                    Render(true);
                    continue;
                }

                switch (ev.Code)
                {
                    case KeyCode.Enter:
                        // finalize input: clear caret artifact by rendering without caret
                        Render(false);
                        // move cursor to beginning of next line (CR+LF)
                        _io.PutChar('\r');
                        _io.PutChar('\n');
                        return buf.ToString();
                    case KeyCode.Backspace:
                        if (caret > 0)
                        {
                            buf.Remove(caret - 1, 1);
                            caret--;
                            Render(true);
                        }
                        break;
                    case KeyCode.Delete:
                        if (caret < buf.Length)
                        {
                            buf.Remove(caret, 1);
                            Render(true);
                        }
                        break;
                    case KeyCode.Left:
                        if (caret > 0) { caret--; Render(true); }
                        break;
                    case KeyCode.Right:
                        if (caret < buf.Length) { caret++; Render(true); }
                        break;
                    case KeyCode.Home:
                        caret = 0; Render(true); break;
                    case KeyCode.End:
                        caret = buf.Length; Render(true); break;
                    case KeyCode.Escape:
                        // Cancel input: clear line and return empty
                        // Wipe visible region
                        for (int i = 0; i < Math.Min(buf.Length + 1, editableCols); i++)
                            _io.WriteTextAt(startCol + i, row, ' ');
                        _io.LocateCursor(startCol, row);
                        return string.Empty;
                    default:
                        break;
                }
            }
            else
            {
                // No key, sleep a bit to avoid busy loop
                _sched.Sleep(TimeSpan.FromMilliseconds(20), ct);
            }
        }
    }
}
