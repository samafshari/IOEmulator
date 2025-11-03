using System;
using System.Runtime.InteropServices;
using Neat;
using NAudio.Wave;
using System.Threading;

namespace Neat.UI;

public sealed class AvaloniaSoundDriver : ISoundDriver
{
    // Simple QBASIC-like PLAY parser and synchronous player using Console.Beep on Windows
    private sealed class MusicState
    {
        public int TempoBpm = 120; // T
        public int DefaultLen = 4; // L (denominator), e.g., 4 = quarter
        public int Octave = 4;     // O
    }

    public void Beep()
    {
        TryBeep(800, 200);
    }

    public void PlayTone(int frequencyHz, int durationMs)
    {
        if (frequencyHz <= 0 || durationMs <= 0) return;
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

    private static void TryBeep(int f, int d)
    {
        // Use shared-mode waveOut (WaveOutEvent) for broad compatibility and easy capture by other apps
        try
        {
            int sampleRate = 44100;
            using var waveOut = new WaveOutEvent { DesiredLatency = 60 };
            using var tone = new SineToneProvider(frequency: f, durationMs: d, sampleRate: sampleRate);
            waveOut.Init(tone);
            using var done = new ManualResetEventSlim(false);
            waveOut.PlaybackStopped += (_, __) => done.Set();
            waveOut.Play();
            // Wait until provider completes
            done.Wait(TimeSpan.FromMilliseconds(Math.Max(d + 100, 200)));
        }
        catch
        {
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

        public SineToneProvider(int frequency, int durationMs, int sampleRate = 44100)
        {
            _format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            _freq = Math.Max(1, frequency);
            _totalSamples = (int)Math.Max(1, Math.Round(durationMs / 1000.0 * sampleRate));
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
                buffer[offset + n] = (float)(_amplitude * Math.Sin(_phase));
                _phase += 2 * Math.PI * _freq / sampleRate;
            }
            _samplesGenerated += samplesToWrite;
            return samplesToWrite;
        }

        public void Dispose() { /* nothing to dispose */ }
    }
}
