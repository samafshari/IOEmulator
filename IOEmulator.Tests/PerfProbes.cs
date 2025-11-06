using System;
using System.Threading.Tasks;
using Xunit;
using Neat;

namespace Neat.Test;

public class PerfProbes
{
    [Fact]
    public void IOEmulatorCtorOnly()
    {
        var io = new IOEmulator();
        Assert.NotNull(io);
    }

    [Fact]
    public void CodePageLoad()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var cp = CodePage.IBM8x8();
        sw.Stop();
        Assert.NotNull(cp);
    }

    [Fact]
    public void PrimitivesOnly()
    {
        var io = new IOEmulator();
        io.SetPixelDimensions(64, 64);
        io.ResetView();
        io.SetColor(1, new RGB(255, 0, 0));
        io.ForegroundColorIndex = 1;
        io.PSet(10, 10, 1);
        io.Line(0, 0, 10, 10);
        var a = io.Point(10, 10);
        Assert.Equal(255, a.R);
    }

    [Fact]
    public void TextWriteOnly()
    {
        var io = new IOEmulator();
        io.LoadQBasicScreenMode(0);
        io.BackgroundColorIndex = 0;
        io.ForegroundColorIndex = 7;
        io.WriteTextAt(0, 0, 'A');
        Assert.True(true);
    }

    [Fact]
    public void InterpreterBasicOnly_NoSound()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io, new TestSilentSound());
        var interp = new QBasicInterpreter(qb);
        var src = "SCREEN 13\nCOLOR 15,1\nCLS\nLOCATE 5,10\nPRINT \"Hello\"\nSLEEP 0\n";
        interp.Run(src);
    }

    private sealed class TestSilentSound : ISoundDriver
    {
        public void Beep() { }
        public void PlayMusicString(string musicString) { }
        public void PlayTone(int frequencyHz, int durationMs) { }
    }
}
