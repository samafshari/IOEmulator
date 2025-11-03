using System;
using System.Runtime.InteropServices;
using Neat;
using NAudio.Wave;
using System.Threading;

namespace Neat.UI;

public sealed class AvaloniaSoundDriver : ISoundDriver, IDisposable
{
    // Simple QBASIC-like PLAY parser and synchronous player using Console.Beep on Windows
    private sealed class MusicState
    {
        public int TempoBpm = 120; // T
        public int DefaultLen = 4; // L (denominator), e.g., 4 = quarter
        public int Octave = 4;     // O
    }

    private readonly object _audioLock = new object();
    private WaveOutEvent? _waveOut;

    public void Beep()
    {
        TryBeep(800, 200);
    }

    public void PlayTone(int frequencyHz, int durationMs)
    {
        if (frequencyHz <= 0 || durationMs <= 0) return;
        System.Diagnostics.Debug.WriteLine($"AvaloniaSoundDriver: PlayTone({frequencyHz}Hz, {durationMs}ms)");
        TryBeep(frequencyHz, durationMs);
    }

    public void PlayMusicString(string musicString)
    {
        if (string.IsNullOrWhiteSpace(musicString)) { return; }
        var st = new MusicState();
        int i = 0;
        while (i < musicString.Length)
        {
            char ch = char.ToUpperInvariant(musicString[i]);
            if (char.IsWhiteSpace(ch) || ch == ',') { i++; continue; }

            // Commands: Tn, Ln, On, <, >, Pn/Rn, Notes A-G[#|+|-][n][.]
            if (ch == 'T') { i++; st.TempoBpm = ReadNumber(musicString, ref i, st.TempoBpm); continue; }
            if (ch == 'L') { i++; st.DefaultLen = Math.Clamp(ReadNumber(musicString, ref i, st.DefaultLen), 1, 64); continue; }
            if (ch == 'O') { i++; st.Octave = Math.Clamp(ReadNumber(musicString, ref i, st.Octave), 0, 8); continue; }
            if (ch == '<') { i++; st.Octave = Math.Max(0, st.Octave - 1); continue; }
            if (ch == '>') { i++; st.Octave = Math.Min(8, st.Octave + 1); continue; }
            if (ch == 'P' || ch == 'R')
            {
                i++;
                int len = ReadNumber(musicString, ref i, st.DefaultLen);
                bool dotted = false;
                if (i < musicString.Length && musicString[i] == '.') { dotted = true; i++; }
                int dur = CalcDurationMs(st.TempoBpm, len, dotted);
                Sleep(dur);
                continue;
            }

            int? semitone = ch switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => (int?)null
            };
            if (semitone.HasValue)
            {
                i++;
                // accidental
                if (i < musicString.Length)
                {
                    if (musicString[i] == '#' || musicString[i] == '+') { semitone++; i++; }
                    else if (musicString[i] == '-') { semitone--; i++; }
                }
                int len = ReadNumber(musicString, ref i, st.DefaultLen);
                bool dotted = false;
                if (i < musicString.Length && musicString[i] == '.') { dotted = true; i++; }
                int dur = CalcDurationMs(st.TempoBpm, len, dotted);
                int freq = NoteToFrequency(st.Octave, semitone.Value);
                TryBeep(freq, dur);
                continue;
            }

            // Unknown token — skip
            i++;
        }
    }

    private void TryBeep(int f, int d)
    {
        // Use shared-mode waveOut (WaveOutEvent) for broad compatibility and easy capture by other apps
        try
        {
            System.Diagnostics.Debug.WriteLine($"AvaloniaSoundDriver: TryBeep start {f}Hz {d}ms");
            int sampleRate = 44100;
            SineToneProvider? tone = null;
            ManualResetEventSlim? done = null;
            EventHandler<StoppedEventArgs>? handler = null;
            lock (_audioLock)
            {
                if (_waveOut == null)
                {
                    System.Diagnostics.Debug.WriteLine("AvaloniaSoundDriver: Creating new WaveOutEvent");
                    _waveOut = new WaveOutEvent { DesiredLatency = 60 };
                }
                tone = new SineToneProvider(frequency: f, durationMs: d, sampleRate: sampleRate);
                _waveOut.Init(tone);
                done = new ManualResetEventSlim(false);
                handler = (_, __) => { System.Diagnostics.Debug.WriteLine("AvaloniaSoundDriver: Playback stopped"); done.Set(); };
                _waveOut.PlaybackStopped += handler;
                _waveOut.Play();
                System.Diagnostics.Debug.WriteLine($"AvaloniaSoundDriver: Play started, waiting for completion...");
            }
            // Wait until provider completes (outside lock)
            bool completed = done!.Wait(TimeSpan.FromMilliseconds(Math.Max(d + 100, 200)));
            System.Diagnostics.Debug.WriteLine($"AvaloniaSoundDriver: Wait completed={completed}");
            // Detach handler
            try { lock (_audioLock) { if (_waveOut != null && handler != null) _waveOut.PlaybackStopped -= handler; } } catch { }
            done.Dispose();
        }
        catch (Exception ex)
        {
            // Log to Debug for diagnostics
            System.Diagnostics.Debug.WriteLine($"AvaloniaSoundDriver: TryBeep failed with {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            // As a fallback, try Console.Beep (Windows only; may be inaudible or exclusive on some systems)
            try { if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Console.Beep(f, d); } catch { }
        }
    }

    private static void Sleep(int ms)
    {
        try { System.Threading.Thread.Sleep(Math.Max(ms, 0)); } catch { }
    }

    private static int ReadNumber(string s, ref int i, int defaultVal)
    {
        int start = i;
        int val = 0;
        while (i < s.Length && char.IsDigit(s[i]))
        {
            val = val * 10 + (s[i] - '0');
            i++;
        }
        return (i > start) ? val : defaultVal;
    }

    private static int CalcDurationMs(int tempoBpm, int lenDenominator, bool dotted)
    {
        // Quarter note duration in ms
        double quarter = 60000.0 / Math.Max(tempoBpm, 1);
        // Lx is denominator: L4 = quarter (1x), L8 = eighth (0.5x), L1 = whole (4x)
        double factor = 4.0 / Math.Max(lenDenominator, 1);
        double dur = quarter * factor;
        if (dotted) dur *= 1.5;
        return (int)Math.Max(1, Math.Round(dur));
    }

    private static int NoteToFrequency(int octave, int semitone)
    {
        // Map to MIDI where A4 (octave 4, A) = 440Hz at MIDI 69, C4 = 60
        int midi = 12 * (octave + 1) + semitone; // C-1 = 0
        double freq = 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
        int f = (int)Math.Round(freq);
        if (f < 37) f = 37; if (f > 32767) f = 32767;
        return f;
    }

    private sealed class SineToneProvider : ISampleProvider, IDisposable
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
            // 2ms linear fade in/out to avoid clicks
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
                {
                    env = (float)globalIndex / _fadeSamples;
                }
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

        public void Dispose() { /* nothing to dispose */ }
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
