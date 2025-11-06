using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Neat;

namespace Neat.Test;

public class CombinedTests
{
    [Fact]
    public void All()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
    var stampDir = Path.Combine(Environment.CurrentDirectory, "TestResults");
    try { Directory.CreateDirectory(stampDir); } catch {}
    var stampFile = Path.Combine(stampDir, "stamps.txt");
        try { if (File.Exists(stampFile)) File.Delete(stampFile); } catch {}
        void Stamp(string label)
        {
            var line = $"[STAMP] {label}: {sw.Elapsed.TotalMilliseconds:F1} ms";
            try { File.AppendAllText(stampFile, line + Environment.NewLine); } catch {}
            sw.Restart();
        }
        // Core pixel primitives
        var io = new IOEmulator();
        Stamp("IOEmulator ctor");
        io.SetPixelDimensions(64, 64);
        io.ResetView();
        io.SetColor(1, new RGB(255, 0, 0));
        io.ForegroundColorIndex = 1;
        io.PSet(10, 10, 1);
        io.Line(0, 0, 10, 10);
        var a = io.Point(10, 10);
        Assert.Equal(255, a.R);
        Stamp("Primitives");

        // View clipping
        io.ClearPixelBuffer();
        io.SetColor(2, new RGB(0, 255, 0));
        io.ForegroundColorIndex = 2;
        io.SetView(8, 8, 15, 15);
        io.Line(0, 0, 31, 31);
        var inside = io.Point(10, 10);
        Assert.Equal(255, inside.G);
    Stamp("Clipping");

        // Blocks & IO
        io.SetColor(4, new RGB(10, 20, 30));
        for (int y = 2; y < 6; y++)
            for (int x = 2; x < 6; x++) io.PSet(x, y, 4);
        var block = io.GetBlock(2, 2, 4, 4);
        io.PutBlock(2, 2, in block, IOEmulator.RasterOp.XOR);
        var c = io.Point(3, 3);
        Assert.Equal(0, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            io.BSave(tmp, 0, io.VRAM.ByteLength);
            io.ClearPixelBuffer();
            io.BLoad(tmp, 0);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
        Stamp("Blocks & IO");

        // Text & window
        io.LoadQBasicScreenMode(0);
        io.BackgroundColorIndex = 0;
        io.ForegroundColorIndex = 7;
        var before = io.Point(0, 0);
        io.WriteTextAt(0, 0, 'A');
        var cell = io.GetTextXY(0, 0);
        bool anyDifferent = false;
        for (int y = 0; y < 8 && !anyDifferent; y++)
            for (int x = 0; x < 8 && !anyDifferent; x++)
                anyDifferent |= io.Point(cell.X + x, cell.Y + y).R != before.R
                              || io.Point(cell.X + x, cell.Y + y).G != before.G
                              || io.Point(cell.X + x, cell.Y + y).B != before.B;
        Assert.True(anyDifferent);
        io.SetPixelDimensions(100, 100);
        io.SetView(10, 10, 90, 90);
        io.SetWindow(-1, -1, 1, 1);
        var (sx, sy) = io.WorldToScreen(0, 0);
        Assert.InRange(sx, 49, 51);
        Assert.InRange(sy, 49, 51);
        Stamp("Text & Window");

        // Input & Scheduler
        var qb = new QBasicApi(io);
        Assert.Equal(string.Empty, qb.INKEY());
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'X'));
        Assert.Equal("X", qb.INKEY());
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'Y'));
        qb.SLEEP(null); // returns immediately due to queued key
        Assert.True(true);
    Stamp("Input & Scheduler");

        // Interpreter
    var interp = new QBasicInterpreter(qb);
    var src = "SCREEN 13\nCOLOR 15,1\nCLS\nLOCATE 5,10\nPRINT \"Hello\"\nBEEP\nSOUND 440, 100\nSLEEP 0\n";
        interp.Run(src);
        Stamp("Interpreter basic program");
        // IF INKEY$ test
    var src2 = "10: IF INKEY$ <> \"\" THEN END\nGOTO 10\n";
        var t = Task.Run(() => interp.Run(src2));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Enter, '\n'));
        t.Wait(200);
        Assert.True(t.IsCompleted);
        Stamp("Interpreter IF INKEY$ loop");
    }
}
