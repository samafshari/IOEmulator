using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Tests;

public class QBasicLanguageTests
{
    [Fact]
    public void LineInput_EmitsCRLF_AndMovesToNextLineStart()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        qb.SCREEN(13);
        qb.COLOR(15, 1);
        qb.CLS();
        // Position somewhere visible
        qb.LOCATE(5, 10);
        int rowBefore = io.CursorY;
        int colBefore = io.CursorX;

        // Start read on background task since LineInput blocks until Enter
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        string? result = null;
        var t = Task.Run(() => result = qb.LineInput("> ", cts.Token));

        // Simulate typing "AB" then Enter
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'A'));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'B'));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Enter));

        Assert.True(t.Wait(TimeSpan.FromSeconds(1)), "LineInput did not complete");
        Assert.Equal("AB", result);
        Assert.Equal(rowBefore + 1, io.CursorY); // moved one line down
        Assert.Equal(0, io.CursorX); // at start of next line (CR+LF)
    }

    [Fact]
    public void LineInput_LeavesNoCaretArtifact_OnSubmit()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        qb.SCREEN(13);
        qb.COLOR(15, 1);
        qb.CLS();
        qb.LOCATE(6, 8);
        int row = io.CursorY;
        int startCol = io.CursorX;

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        string? result = null;
        var t = Task.Run(() => result = qb.LineInput("> ", cts.Token));

        // Type "AB" and submit
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'A'));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'B'));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Enter));

        Assert.True(t.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("AB", result);

        // Verify that beyond the typed text (prompt 2 + 2 chars), the rest of the row cell band is background only
        int charWidth = io.ResolutionW / io.TextCols;
        int charHeight = io.ResolutionH / io.TextRows;
        var rowTop = io.GetTextXY(0, row).Y;
        int textEndX = io.GetTextXY(startCol + 4, row).X; // 2 prompt + 2 typed
        var bg = io.GetColor(io.BackgroundColorIndex);
        for (int y = rowTop; y < rowTop + charHeight; y++)
        {
            for (int x = textEndX; x < io.ResolutionW; x++)
            {
                var c = io.PixelBuffer[y * io.ResolutionW + x];
                Assert.True(c.R == bg.R && c.G == bg.G && c.B == bg.B, "Found non-background pixel after text end (caret artifact)");
            }
        }
    }

    [Fact]
    public void GuessGame_Flow_WithColonSplit_LoopsThenFinishes()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        // Fixed T to 50 for determinism
    string program = @"SCREEN 13
COLOR 15,1
CLS
T = 50
10: LINE INPUT "" > ""; A$
N = VAL(A$)
IF N < T THEN PRINT ""Too low"" : GOTO 10
IF N > T THEN PRINT ""Too high"" : GOTO 10
IF N = T THEN PRINT ""You got it!"" : END
";
        var run = Task.Run(() => interp.Run(program));
        // First guess: 30 (too low), should loop back to 10 awaiting input
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, '3'));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, '0'));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Enter));
        // Give a moment then send the correct guess
        Task.Delay(50).Wait();
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, '5'));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, '0'));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Enter));

        Assert.True(run.Wait(TimeSpan.FromSeconds(2)), "Interpreter did not terminate after correct guess");
    }
}
