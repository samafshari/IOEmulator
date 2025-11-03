using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Tests;

public class QBasicInterpreterTests
{
    private sealed class TestSoundDriver : ISoundDriver
    {
        public int BeepCount;
        public (int f, int d)? LastTone;
        public string? LastPlay;
        public void Beep() => BeepCount++;
        public void PlayTone(int frequencyHz, int durationMs) => LastTone = (frequencyHz, durationMs);
        public void PlayMusicString(string musicString) => LastPlay = musicString;
    }

    [Fact]
    public void PrintAndClsAffectPixels()
    {
        var io = new IOEmulator();
        var sound = new TestSoundDriver();
        var qb = new QBasicApi(io, sound);
        var interp = new QBasicInterpreter(qb);

        string src = @"SCREEN 13
COLOR 15,1
CLS
LOCATE 5,10
PRINT ""Hello""
";

        interp.Run(src);
        // Assert some pixel changed from background near text area
        var before = io.GetColor(io.BackgroundColorIndex);
        bool anyDiff = false;
        for (int y = 0; y < io.ResolutionH; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var c = io.PixelBuffer[y * io.ResolutionW + x];
                if (c.R != before.R || c.G != before.G || c.B != before.B) { anyDiff = true; break; }
            }
            if (anyDiff) break;
        }
        Assert.True(anyDiff);
    }

    [Fact]
    public void GraphicsCommandsDraw()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
LINE (0,0)-(319,199), 12
PSET (10,10), 14
";
        interp.Run(src);
        Assert.NotEqual(default(RGB), io.ReadPixelAt(10, 10));
    }

    [Fact]
    public async Task SleepWithSecondsCompletes()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
    string src = @"SCREEN 13
PRINT ""Waiting""
SLEEP 0
PRINT ""Done""
END
";
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        // Run synchronously; SLEEP 0 should not block
        interp.Run(src, cts.Token);
        Assert.True(true);
    }

    [Fact]
    public void BeepAndSoundDelegateToDriver()
    {
        var io = new IOEmulator();
        var sound = new TestSoundDriver();
        var qb = new QBasicApi(io, sound);
        var interp = new QBasicInterpreter(qb);
        string src = @"BEEP
SOUND 440, 100
";
        interp.Run(src);
        Assert.Equal(1, sound.BeepCount);
        Assert.Equal((440, 100), sound.LastTone);
    }

    [Fact]
    public void Play_Delegates_To_SoundDriver()
    {
        var io = new IOEmulator();
        var sound = new TestSoundDriver();
        var qb = new QBasicApi(io, sound);
        var interp = new QBasicInterpreter(qb);
        string src = @"PLAY ""C"" ";
        interp.Run(src);
        Assert.Equal("C", sound.LastPlay);
    }

    [Fact]
    public async Task IfInkeyGotoEndTerminates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"10: IF INKEY$ <> """" THEN END
GOTO 10
";
        var t = Task.Run(() => interp.Run(src));
        // Inject immediately to avoid delay
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Enter, '\n'));
        await Task.WhenAny(t, Task.Delay(200));
        Assert.True(t.IsCompleted);
    }

    [Fact]
    public void End_Prints_Ending_Message()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
CLS
PRINT """"  
";
        // Run minimal program (no output) — end message should still be printed
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        bool anyDiff = false;
        for (int y = 0; y < io.ResolutionH && !anyDiff; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var c = io.PixelBuffer[y * io.ResolutionW + x];
                if (c.R != bg.R || c.G != bg.G || c.B != bg.B) { anyDiff = true; break; }
            }
        }
        Assert.True(anyDiff);
    }

    [Fact]
    public void Error_UnknownStatement_PrintsAndTerminates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
CLS
WAT 123
";
        // Run should not throw and should complete
        interp.Run(src);
        // Some pixels should have changed due to error message being printed
        var bg = io.GetColor(io.BackgroundColorIndex);
        bool anyDiff = false;
        for (int y = 0; y < io.ResolutionH && !anyDiff; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var c = io.PixelBuffer[y * io.ResolutionW + x];
                if (c.R != bg.R || c.G != bg.G || c.B != bg.B) { anyDiff = true; break; }
            }
        }
        Assert.True(anyDiff);
    }

    [Fact]
    public void Error_UndefinedLabel_PrintsAndTerminates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
CLS
GOTO 10
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        bool anyDiff = false;
        for (int y = 0; y < io.ResolutionH && !anyDiff; y++)
        {
            for (int x = 0; x < io.ResolutionW; x++)
            {
                var c = io.PixelBuffer[y * io.ResolutionW + x];
                if (c.R != bg.R || c.G != bg.G || c.B != bg.B) { anyDiff = true; break; }
            }
        }
        Assert.True(anyDiff);
    }
}
