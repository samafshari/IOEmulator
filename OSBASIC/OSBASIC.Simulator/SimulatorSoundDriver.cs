using System;
using System.Threading;
using Neat;
using NAudio.Wave;

namespace OSBASIC.Simulator
{
    // Simple NAudio-based sound driver for Simulator
    internal sealed class SimulatorSoundDriver : ISoundDriver, IDisposable
    {
        private readonly object _audioLock = new object();
        private WaveOutEvent _waveOut;

        public void Beep() => TryBeep(800, 200);

        public void PlayTone(int frequencyHz, int durationMs)
        {
            if (frequencyHz <= 0 || durationMs <= 0) return;
            TryBeep(frequencyHz, durationMs);
        }

        public void PlayMusicString(string musicString)
        {
            // Minimal: just a beep for now; can be extended to parse PLAY strings
            Beep();
        }

        private void TryBeep(int f, int d)
        {
            try
            {
                SineToneProvider tone;
                System.Threading.ManualResetEventSlim done = new(false);
                EventHandler<StoppedEventArgs> handler = null;
                lock (_audioLock)
                {
                    _waveOut ??= new WaveOutEvent { DesiredLatency = 60 };
                    tone = new SineToneProvider(frequency: f, durationMs: d);
                    _waveOut.Init(tone);
                    handler = (_, __) => done.Set();
                    _waveOut.PlaybackStopped += handler;
                    _waveOut.Play();
                }
                // Wait a little longer than duration, then detach
                done.Wait(TimeSpan.FromMilliseconds(Math.Max(d + 100, 200)));
                try { lock (_audioLock) { if (_waveOut != null && handler != null) _waveOut.PlaybackStopped -= handler; } } catch { }
                done.Dispose();
            }
            catch
            {
                // ignore audio errors in simulator
            }
        }

        private sealed class SineToneProvider : ISampleProvider
        {
            private readonly WaveFormat _format;
            private readonly float _amplitude = 0.25f;
            private readonly double _freq;
            private readonly int _totalSamples;
            private int _samplesGenerated;
            private double _phase;
            private readonly int _fadeSamples;

            public SineToneProvider(int frequency, int durationMs, int sampleRate = 44100)
            {
                _format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
                _freq = Math.Max(1, frequency);
                _totalSamples = (int)Math.Max(1, Math.Round(durationMs / 1000.0 * sampleRate));
                _fadeSamples = Math.Max(1, (int)(0.002 * sampleRate));
            }

            public WaveFormat WaveFormat => _format;

            public int Read(float[] buffer, int offset, int count)
            {
                int samplesRemaining = _totalSamples - _samplesGenerated;
                if (samplesRemaining <= 0) return 0;
                int samplesToWrite = Math.Min(count, samplesRemaining);
                double sampleRate = _format.SampleRate;
                for (int n = 0; n < samplesToWrite; n++)
                {
                    float env;
                    int globalIndex = _samplesGenerated + n;
                    if (globalIndex < _fadeSamples)
                        env = (float)globalIndex / _fadeSamples;
                    else if (globalIndex > _totalSamples - _fadeSamples)
                    {
                        int remain = _totalSamples - globalIndex;
                        env = Math.Max(0f, (float)remain / _fadeSamples);
                    }
                    else env = 1f;

                    buffer[offset + n] = (float)(_amplitude * env * Math.Sin(_phase));
                    _phase += 2 * Math.PI * _freq / sampleRate;
                }
                _samplesGenerated += samplesToWrite;
                return samplesToWrite;
            }
        }

        public void Dispose()
        {
            try
            {
                lock (_audioLock)
                {
                    _waveOut?.Stop();
                    _waveOut?.Dispose();
                    _waveOut = null;
                }
            }
            catch { }
        }
    }
}
