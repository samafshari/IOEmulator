using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Tests;

public class FractalSampleTests
{
    [Fact(Timeout = 10000)]
    public async Task Fractal_Sample_Runs_And_Renders_Pixels()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb)
            {
                SpeedFactor = 100.0
            };

            string src = QBasicSamples.Load("FRACTAL");
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
            interp.Run(src, cts.Token);

            var bg = io.GetColor(io.BackgroundColorIndex);
            int changed = 0;
            var buf = io.PixelBuffer;
            for (int i = 0; i < buf.Length; i += Math.Max(1, buf.Length / 5000))
            {
                var p = buf[i];
                if (p.R != bg.R || p.G != bg.G || p.B != bg.B) { changed++; }
            }
            Assert.True(changed > 10, $"Expected fractal to draw pixels, found {changed}");
        });
    }
}
