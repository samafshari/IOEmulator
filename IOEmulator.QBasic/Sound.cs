using System;
using System.Runtime.Versioning;

namespace Neat;

public interface ISoundDriver
{
    void Beep();
    void PlayTone(int frequencyHz, int durationMs);
    void PlayMusicString(string musicString);
}

// Minimal default driver: on Windows, uses Console.Beep for tones; otherwise no-op
public class ConsoleBeepSoundDriver : ISoundDriver
{
    public void Beep()
    {
        TryBeep(800, 200);
    }

    public void PlayTone(int frequencyHz, int durationMs)
    {
        if (frequencyHz < 37) frequencyHz = 37;
        if (frequencyHz > 32767) frequencyHz = 32767;
        if (durationMs < 0) durationMs = 0;
        TryBeep(frequencyHz, durationMs);
    }

    public void PlayMusicString(string musicString)
    {
        // Placeholder: parse a tiny subset later. For now, just a simple beep to indicate activity.
        Beep();
    }

    private void TryBeep(int freq, int dur)
    {
        try { Console.Beep(freq, dur); }
        catch { /* ignore on unsupported hosts */ }
    }
}
