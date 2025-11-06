using System;
using Microsoft.JSInterop;
using Neat;

namespace OSBASIC.Browser.Pages
{
    // ISoundDriver implementation that forwards to wwwroot/js/webaudio.js via JS interop
    internal sealed class WebAudioSoundDriver : ISoundDriver
    {
        private readonly IJSRuntime _js;

        public WebAudioSoundDriver(IJSRuntime js)
        {
            _js = js;
        }

        public void Beep()
        {
            TryInvoke("webAudio.beep");
        }

        public void PlayTone(int frequencyHz, int durationMs)
        {
            if (frequencyHz < 37) frequencyHz = 37;
            if (durationMs < 0) durationMs = 0;
            TryInvoke("webAudio.playTone", frequencyHz, durationMs);
        }

        public void PlayMusicString(string musicString)
        {
            if (string.IsNullOrEmpty(musicString)) { Beep(); return; }
            TryInvoke("webAudio.playMusicString", musicString);
        }

        private void TryInvoke(string identifier, params object[] args)
        {
            try
            {
                // Fire-and-forget; sound should not block interpreter
                _ = _js.InvokeVoidAsync(identifier, args);
            }
            catch
            {
                // Ignore if JS not available or function missing
            }
        }
    }
}
