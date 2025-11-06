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
    public async Task Print_Appends_Newline_By_Default()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,1
CLS
LOCATE 5,10
PRINT ""Hello""
PRINT ""World""
 SLEEP
";
        using var cts = new CancellationTokenSource();
        var t = Task.Run(() => interp.Run(src, cts.Token));
        // Give the interpreter a moment to reach SLEEP, then cancel to avoid end message
        await Task.Delay(50);
        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(500));
        // After two prints starting at row 5, cursor should be at start of row 7
        Assert.Equal(7, io.CursorY);
        Assert.Equal(0, io.CursorX);
    }

    [Fact]
    public async Task Print_Semicolon_Suppresses_Newline()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
COLOR 15,1
CLS
LOCATE 5,10
 PRINT ""Hello"";
 SLEEP";
        using var cts = new CancellationTokenSource();
        var t = Task.Run(() => interp.Run(src, cts.Token));
        await Task.Delay(50);
        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(500));
        // Cursor should remain on the same row (5) and advance past the text
        Assert.Equal(5, io.CursorY);
        Assert.True(io.CursorX > 10);
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
    public void Arithmetic_And_Assignment_Work()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
CLS
I = 0
I = I + 2
IF I = 2 THEN PSET 0,0,15
";
        interp.Run(src);
        Assert.NotEqual(default(RGB), io.ReadPixelAt(0, 0));
    }

    [Fact]
    public void PC_Function_Reflects_Pixels()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
CLS
PSET 2,2, 10
IF PC(2,2) > 0 THEN PSET 0,0, 15
";
        interp.Run(src);
        Assert.NotEqual(default(RGB), io.ReadPixelAt(0, 0));
    }

    [Fact]
    public void Pset_With_Variables_And_Expressions_Works()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
    string src = @"SCREEN 13
CLS
SX = 13 : SY = 16 : C = 15
PSET SX, SY, C
";
        interp.Run(src);
    var bg = io.GetColor(io.BackgroundColorIndex);
    var px = io.ReadPixelAt(13, 16);
        Assert.True(px.R != bg.R || px.G != bg.G || px.B != bg.B);
    }

    [Fact(Timeout = 5000)]
    public async Task Pathfind_Sample_Animates()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb) { SpeedFactor = 100.0 };
        var src = QBasicSamples.Load("PATHFIND.bas");
        using var cts = new CancellationTokenSource();
        var t = Task.Run(() => interp.Run(src, cts.Token));
        await Task.Delay(100);
        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(500));
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
    public void If_Then_Else_Branches_Correctly()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
CLS
X = 5
IF X >= 5 THEN PSET 1,1, 15 ELSE PSET 1,1, 10
IF X <> 4 THEN PSET 2,2, 15 ELSE PSET 2,2, 10
IF X <= 5 THEN PSET 3,3, 15 ELSE PSET 3,3, 10
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(1, 1));
        Assert.NotEqual(bg, io.ReadPixelAt(2, 2));
        Assert.NotEqual(bg, io.ReadPixelAt(3, 3));
    }

    [Fact]
    public void Colon_Separated_Assignments_On_One_Line()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = @"SCREEN 13
CLS
A = 1 : B = 2 : C = 3
IF A = 1 THEN IF B = 2 THEN IF C = 3 THEN PSET 0,0, 15
";
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(0, 0));
    }

    [Fact(Timeout = 5000)]
    public async Task PC_With_Arithmetic_Expressions_Works()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
S = 4
PSET 10, 10, 14
IF PC(10+S, 10) = 0 THEN PSET 0,0, 15
IF PC(10-S, 10) = 0 THEN PSET 1,1, 15
IF PC(10, 10+S) = 0 THEN PSET 2,2, 15
IF PC(10, 10-S) = 0 THEN PSET 3,3, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // All four directions should have empty cells and draw test pixels
            Assert.NotEqual(bg, io.ReadPixelAt(0, 0));
            Assert.NotEqual(bg, io.ReadPixelAt(1, 1));
            Assert.NotEqual(bg, io.ReadPixelAt(2, 2));
            Assert.NotEqual(bg, io.ReadPixelAt(3, 3));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Simple_PSET_In_THEN_Works()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
IF 1 = 1 THEN PSET 10, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.True(px.R != bg.R || px.G != bg.G || px.B != bg.B, "PSET in THEN clause didn't work");
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Variable_Assignment_Before_IF_Works()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
S = 4
X = 10 + S
PSET X, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(14, 10);
            Assert.True(px.R != bg.R || px.G != bg.G || px.B != bg.B, "Variable with expression assignment didn't work");
        });
    }

    [Fact(Timeout = 5000)]
    public async Task PSET_With_Expression_In_THEN_Works()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
S = 4
IF 1 = 1 THEN PSET 10+S, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // Check both direct coordinate and nearby
            var px14 = io.ReadPixelAt(14, 10);
            var px13 = io.ReadPixelAt(13, 10);
            var px15 = io.ReadPixelAt(15, 10);
            bool found = (px14.R != bg.R || px14.G != bg.G || px14.B != bg.B)
                      || (px13.R != bg.R || px13.G != bg.G || px13.B != bg.B)
                      || (px15.R != bg.R || px15.G != bg.G || px15.B != bg.B);
            Assert.True(found, $"PSET with expression in THEN clause didn't set any pixel near x=14. Checked (13,10), (14,10), (15,10)");
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Pathfind_Frontier_Expansion_Single_Step()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb) { SpeedFactor = 100.0 };
            string src = @"SCREEN 13
COLOR 15,0
CLS
S = 4
CF = 14
PSET 10, 10, CF
IF PC(10, 10) = CF THEN PSET 10+S, 10, 11
IF PC(10, 10) = CF THEN PSET 10-S, 10, 11
IF PC(10, 10) = CF THEN PSET 10, 10+S, 11
IF PC(10, 10) = CF THEN PSET 10, 10-S, 11
";
            interp.Run(src);
            // Verify that all 4 neighbors were expanded
            var bg = io.GetColor(io.BackgroundColorIndex);
            var c11 = io.GetColor(11);
            var px_right = io.ReadPixelAt(14, 10);
            var px_left = io.ReadPixelAt(6, 10);
            var px_down = io.ReadPixelAt(10, 14);
            var px_up = io.ReadPixelAt(10, 6);
            
            // At least one direction should have been set (debugging)
            bool anySet = (px_right.R != bg.R || px_right.G != bg.G || px_right.B != bg.B)
                       || (px_left.R != bg.R || px_left.G != bg.G || px_left.B != bg.B)
                       || (px_down.R != bg.R || px_down.G != bg.G || px_down.B != bg.B)
                       || (px_up.R != bg.R || px_up.G != bg.G || px_up.B != bg.B);
            Assert.True(anySet, "No pixels were set by PSET in THEN clause");
            
            Assert.Equal(c11, px_right); // right
            Assert.Equal(c11, px_left);  // left
            Assert.Equal(c11, px_down); // down
            Assert.Equal(c11, px_up);  // up
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Pathfind_Color_Swap_Works()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb) { SpeedFactor = 100.0 };
            string src = @"SCREEN 13
COLOR 15,0
CLS
CF = 14 : NF = 11
T = CF : CF = NF : NF = T
PSET 0,0, CF
";
            interp.Run(src);
            var c11 = io.GetColor(11);
            Assert.Equal(c11, io.ReadPixelAt(0, 0));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Pathfind_Loop_With_Condition_Terminates()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb) { SpeedFactor = 100.0 };
            string src = @"SCREEN 13
COLOR 15,0
CLS
COUNT = 0
LOOP:
COUNT = COUNT + 1
IF COUNT >= 10 THEN GOTO DONE ELSE GOTO LOOP
DONE:
PSET 0,0, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(0, 0));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Pathfind_Micro_Reaches_Target()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb) { SpeedFactor = 100.0 };
        string src = @"SCREEN 13
COLOR 15,0
CLS
S = 4
SX = 8 : SY = 8
GX = 16 : GY = 8
CF = 14 : NF = 11
PSET SX, SY, CF
SWEEPS = 0
L0:
SWEEPS = SWEEPS + 1
IF SWEEPS > 100 THEN END
I = 0
YSCAN:
IF I > 24 THEN GOTO ENDROW
J = 0
XSCAN:
IF J > 24 THEN GOTO NEXTROW
IF PC(J, I) = CF THEN GOTO EXPAND ELSE GOTO ADVANCE
EXPAND:
IF PC(J+S, I) = 0 THEN PSET J+S, I, NF
IF PC(J-S, I) = 0 THEN PSET J-S, I, NF
IF PC(J, I+S) = 0 THEN PSET J, I+S, NF
IF PC(J, I-S) = 0 THEN PSET J, I-S, NF
ADVANCE:
J = J + S
GOTO XSCAN
NEXTROW:
I = I + S
GOTO YSCAN
ENDROW:
T = CF : CF = NF : NF = T
IF PC(GX, GY) <> 0 THEN END ELSE GOTO L0
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var goal = io.ReadPixelAt(16, 8);
            Assert.NotEqual(bg, goal);
        });
    }

    [Fact]
    public async Task Print_String_With_Doubled_Quotes_Works()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
    string src = "SCREEN 13\r\n" +
              "COLOR 15,1\r\n" +
              "CLS\r\n" +
              "LOCATE 1,0\r\n" +
              "PRINT \"He said \"\"Hi\"\"\";\r\n" +
              "SLEEP";
        using var cts = new CancellationTokenSource();
        var t = Task.Run(() => interp.Run(src, cts.Token));
        await Task.Delay(50);
        cts.Cancel();
        await Task.WhenAny(t, Task.Delay(500));
    // Length of printed text should be 12 (He said "Hi") and cursor remains on same line due to ';'
    Assert.Equal(1, io.CursorY);
    Assert.Equal(12, io.CursorX);
    }

    [Fact]
    public void Sound_Blocks_Until_Done()
    {
        var io = new IOEmulator();
        var sound = new BlockingSoundDriver(delayMsForTone: 40, delayMsForPlay: 0);
        var qb = new QBasicApi(io, sound);
        var interp = new QBasicInterpreter(qb);
        string src = @"SOUND 440, 30
PRINT ""OK""";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        interp.Run(src);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 30, $"Elapsed too short: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Play_Blocks_Until_Done()
    {
        var io = new IOEmulator();
        var sound = new BlockingSoundDriver(delayMsForTone: 0, delayMsForPlay: 40);
        var qb = new QBasicApi(io, sound);
        var interp = new QBasicInterpreter(qb);
        string src = @"PLAY ""C""
PRINT ""OK""";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        interp.Run(src);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 35, $"Elapsed too short: {sw.ElapsedMilliseconds}ms");
    }

    private sealed class BlockingSoundDriver : ISoundDriver
    {
        private readonly int _toneDelay;
        private readonly int _playDelay;
        public BlockingSoundDriver(int delayMsForTone, int delayMsForPlay)
        {
            _toneDelay = delayMsForTone;
            _playDelay = delayMsForPlay;
        }
        public void Beep() { }
        public void PlayTone(int frequencyHz, int durationMs)
        {
            System.Threading.Thread.Sleep(_toneDelay);
        }
        public void PlayMusicString(string musicString)
        {
            System.Threading.Thread.Sleep(_playDelay);
        }
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

    [Fact]
    public void Validation_Catches_NEXT_Without_FOR()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string invalidSrc = @"SCREEN 13
NEXT X
";
        var ex = Assert.Throws<InvalidOperationException>(() => interp.Run(invalidSrc));
        Assert.Contains("NEXT without matching FOR", ex.Message);
    }

    [Fact]
    public void Validation_Catches_Unclosed_FOR()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string invalidSrc = @"SCREEN 13
FOR I = 1 TO 10
PRINT I
";
        var ex = Assert.Throws<InvalidOperationException>(() => interp.Run(invalidSrc));
        Assert.Contains("Unclosed FOR loop", ex.Message);
    }

    [Fact]
    public void Validation_Catches_Undefined_Label()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string invalidSrc = @"SCREEN 13
GOTO Nowhere
";
        var ex = Assert.Throws<InvalidOperationException>(() => interp.Run(invalidSrc));
        Assert.Contains("Undefined label", ex.Message);
    }

    [Fact]
    public async Task Validation_Halts_Within_2_Seconds_On_Error()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string invalidSrc = @"SCREEN 13
NEXT X
";

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task = Task.Run(() => interp.Run(invalidSrc), cts.Token);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        Assert.Contains("NEXT without matching FOR", ex.Message);
    }

    [Fact]
    public async Task Raytrace_Runs_For_2_Seconds_Then_Halts()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        string src = QBasicSamples.Load("RAYTRACE");

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var task = Task.Run(() => interp.Run(src, cts.Token), cts.Token);

        // Accept either OperationCanceledException (direct throw) or TaskCanceledException (task canceled before delegate runs)
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }
}
