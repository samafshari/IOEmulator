using System;
using System.Runtime.InteropServices;
using Neat;

namespace Neat.UI;

public sealed class AvaloniaSoundDriver : ISoundDriver
{
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
        // Minimal stub: just a beep for now
        Beep();
    }

    private static void TryBeep(int f, int d)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Beep(f, d);
            }
            // Other platforms: no-op for now
        }
        catch
        {
            // ignore audio errors
        }
    }
}
