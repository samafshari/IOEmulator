using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Tests;

public class DemoSampleTests
{
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

            // Verify that some pixels differ from the background color
            var bg = io.GetColor(io.BackgroundColorIndex);
            int changed = 0;
            var buf = io.PixelBuffer;
            for (int i = 0; i < buf.Length; i += Math.Max(1, buf.Length / 5000))
            {
                var p = buf[i];
                if (p.R != bg.R || p.G != bg.G || p.B != bg.B) { changed++; }
            }
            Assert.True(changed > 10, $"Expected some drawn pixels, found {changed} changed samples");
        });
    }
}
