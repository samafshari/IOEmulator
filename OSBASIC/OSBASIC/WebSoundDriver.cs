using Neat;

namespace OSBASIC
{
    // Minimal no-op sound driver for Web (OpenSilver) until WebAudio is wired.
    public class WebSoundDriver : ISoundDriver
    {
        public void Beep()
        {
            // TODO: Use WebAudio via JS interop; for now, no-op to avoid blocking.
        }

        public void PlayTone(int frequencyHz, int durationMs)
        {
            // TODO: Implement tone playback using WebAudio; no-op placeholder.
        }

        public void PlayMusicString(string musicString)
        {
            // TODO: Parse and play simple music strings; no-op placeholder.
        }
    }
}
