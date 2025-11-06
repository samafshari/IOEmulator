using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Test;

public class DemoSampleTests
{
    private class TestSoundDriver : ISoundDriver
    {
        public int LastFreq;
        public int LastDuration;
        public int CallCount;

        public void Beep() { CallCount++; }
        
        public void PlayTone(int frequencyHz, int durationMs)
        {
            LastFreq = frequencyHz;
            LastDuration = durationMs;
            CallCount++;
        }

        public void PlayMusicString(string musicString) { }
    }

    [Fact(Timeout = 10000)]
    public async Task Demo_Sample_Runs_And_Renders_Pixels()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb)
            {
                SpeedFactor = 100.0
            };

            // Load the DEMO sample and run briefly with cancellation
            string src = QBasicSamples.Load("DEMO");
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            interp.Run(src, cts.Token);

            // Verify that some pixels differ from the background index
            int bgIdx = io.BackgroundColorIndex;
            int changed = 0;
            var buf = io.IndexBuffer;
            for (int i = 0; i < buf.Length; i += Math.Max(1, buf.Length / 5000))
            {
                if (buf[i] != bgIdx) { changed++; }
            }
            Assert.True(changed > 10, $"Expected some drawn pixels, found {changed} changed samples");
        });
    }

    [Fact]
    public void SOUND_Evaluates_Expression_Arguments()
    {
        var io = new IOEmulator();
        io.LoadQBasicScreenMode(13);
        var driver = new TestSoundDriver();
        var qb = new QBasicApi(io, driver);
        var interp = new QBasicInterpreter(qb);

        var src = @"
T = 10
SOUND 200 + T, 50 + T
END
";
        interp.Run(src);
        
        Assert.Equal(1, driver.CallCount);
        Assert.Equal(210, driver.LastFreq);
        Assert.Equal(60, driver.LastDuration);
    }

    [Fact]
    public void SOUND_Evaluates_Complex_Expressions_With_Functions()
    {
        var io = new IOEmulator();
        io.LoadQBasicScreenMode(13);
        var driver = new TestSoundDriver();
        var qb = new QBasicApi(io, driver);
        var interp = new QBasicInterpreter(qb);

        var src = @"
T = 0
SOUND 200 + (SIN(T*9) + 100), 50
END
";
        interp.Run(src);
        
        Assert.Equal(1, driver.CallCount);
        // SIN(0) = 0 scaled to 0, so 200 + (0 + 100) = 300
        Assert.Equal(300, driver.LastFreq);
        Assert.Equal(50, driver.LastDuration);
    }
}
