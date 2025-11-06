using System;
using Neat;

namespace Neat.UI;

// Minimal sound driver for Web: no-op for now (placeholder). You can later plug WebAudio via JS interop.
public sealed class WebSoundDriver : ISoundDriver
{
    public void Beep() { /* TODO: WebAudio beep */ }
    public void PlayTone(int frequencyHz, int durationMs) { /* TODO: WebAudio tone */ }
    public void PlayMusicString(string musicString) { /* TODO: Parse and schedule WebAudio */ }
}
