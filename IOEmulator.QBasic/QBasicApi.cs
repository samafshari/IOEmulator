using System;
using System.Threading;

namespace Neat;

// Minimal QBASIC-like facade over IOEmulator
public class QBasicApi
{
    private readonly IOEmulator io;
    private readonly QBasicScheduler scheduler;
    private readonly ISoundDriver sound;
    private readonly LineEditor lineEditor;

    public Action<string>? PrintHook;

    public QBasicApi(IOEmulator emulator, ISoundDriver? soundDriver = null)
    {
        io = emulator ?? throw new ArgumentNullException(nameof(emulator));
    scheduler = new QBasicScheduler(io);
        sound = soundDriver ?? new ConsoleBeepSoundDriver();
    lineEditor = new LineEditor(io, scheduler);
    }

    // Expose the underlying emulator for advanced scenarios (e.g., interpreter control)
    public IOEmulator Emulator => io;

    // Helper to query key state by human-readable name used in QB programs
    public bool EmulatorKeyState(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim().ToUpperInvariant();
        return n switch
        {
            "LEFT" => io.IsKeyDown(KeyCode.Left),
            "RIGHT" => io.IsKeyDown(KeyCode.Right),
            "UP" => io.IsKeyDown(KeyCode.Up),
            "DOWN" => io.IsKeyDown(KeyCode.Down),
            "ENTER" => io.IsKeyDown(KeyCode.Enter),
            "TAB" => io.IsKeyDown(KeyCode.Tab),
            "ESC" or "ESCAPE" => io.IsKeyDown(KeyCode.Escape),
            _ => false
        };
    }

    // SCREEN mode selection
    public void SCREEN(int mode)
    {
        io.LoadQBasicScreenMode(mode);
        io.ResetView();
        io.ResetWindow();
    }

    // COLOR fg[, bg]
    public void COLOR(int fg, int? bg = null)
    {
        io.ForegroundColorIndex = fg;
        if (bg.HasValue) io.BackgroundColorIndex = bg.Value;
    }

    // LOCATE row, col
    public void LOCATE(int row, int col)
    {
        io.LocateCursor(col, row);
    }

    public void PRINT(string s)
    {
        PrintHook?.Invoke(s);
        io.PutString(s);
    }

    // Interactive line input with editing, blinking caret, and basic navigation
    public string LineInput(string prompt = "", CancellationToken cancellationToken = default, LineEditor.Options? options = null)
    {
        return lineEditor.ReadLine(prompt, cancellationToken, options);
    }

    public void CLS()
    {
        io.Cls();
    }

    // INKEY$ equivalent: non-blocking; returns "" if no key; returns 1-char string for printable keys
    public string INKEY()
    {
        KeyEvent ev;
        if (io.TryReadKey(out ev))
        {
            if (ev.Type == KeyEventType.Down)
            {
                // Printable character
                if (ev.Char.HasValue)
                    return ev.Char.Value.ToString();
                // Map a few control keys to classic control characters
                return ev.Code switch
                {
                    KeyCode.Backspace => "\b",
                    KeyCode.Enter => "\n", // treat as LF; emulator handles line feed
                    KeyCode.Tab => "\t",
                    KeyCode.Escape => "\x1B",
                    _ => string.Empty
                };
            }
        }
        return string.Empty;
    }

    // SLEEP [seconds]
    public void SLEEP(int? seconds = null)
    {
        if (seconds == null)
        {
            // QBASIC semantics: SLEEP without argument waits for a keypress
            scheduler.WaitForKey();
            return;
        }
        // For non-positive values, treat as immediate return (useful for tests and matches practical expectations)
        if (seconds.Value <= 0)
        {
            return;
        }
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds.Value));
        try { scheduler.WaitForKey(cts.Token); }
        catch (OperationCanceledException) { /* timeout */ }
    }

    // Overload for interpreter: wait for key with cancellation support
    public void SLEEP(CancellationToken cancellationToken)
    {
        scheduler.WaitForKey(cancellationToken);
    }

    // VIEW (pixel coordinates)
    public void VIEW(int x1, int y1, int x2, int y2) => io.SetView(x1, y1, x2, y2);
    public void VIEW() => io.ResetView();

    // WINDOW (world coordinates)
    public void WINDOW(double wx1, double wy1, double wx2, double wy2) => io.SetWindow(wx1, wy1, wx2, wy2);
    public void WINDOW() => io.ResetWindow();

    // Graphics primitives
    public void PSET(int x, int y, int color) => io.PSet(x, y, color);
    // POINT now returns the palette index (byte promoted to int) at the given pixel, or background index for OOB
    public int POINT(int x, int y)
    {
        if (x < 0 || y < 0 || x >= io.ResolutionW || y >= io.ResolutionH)
            return io.BackgroundColorIndex;
        return io.Point(x, y);
    }
    public void LINE(int x1, int y1, int x2, int y2, int? color = null) => io.Line(x1, y1, x2, y2, color);

    // GET/PUT
    public IOEmulator.ImageBlock GET(int x, int y, int width, int height) => io.GetBlock(x, y, width, height);
    public void PUT(int x, int y, in IOEmulator.ImageBlock block, IOEmulator.RasterOp op = IOEmulator.RasterOp.PSET)
        => io.PutBlock(x, y, block, op);

    // BLOAD/BSAVE
    public void BLOAD(string path, int offset) => io.BLoad(path, offset);
    public void BSAVE(string path, int offset, int length) => io.BSave(path, offset, length);

    // ===== Sound and Music =====
    public void BEEP() => sound.Beep();

    public void SOUND(int frequencyHz, int durationMs)
        => sound.PlayTone(frequencyHz, durationMs);

    public void PLAY(string musicString)
        => sound.PlayMusicString(musicString);
}
