using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SDL2;
using Neat;

namespace Neat.SDLApp;

public class SdlSoundDriver : ISoundDriver, IDisposable
{
    private uint _device;
    private int _sampleRate = 48000;
    private ushort _format = SDL.AUDIO_F32;
    private byte _channels = 1; // mono

    public SdlSoundDriver()
    {
        SDL.SDL_AudioSpec desired = new SDL.SDL_AudioSpec
        {
            freq = _sampleRate,
            format = _format,
            channels = _channels,
            samples = 1024
        };
        SDL.SDL_AudioSpec obtained;
        _device = SDL.SDL_OpenAudioDevice(null, 0, ref desired, out obtained, 0);
        if (_device == 0)
        {
            Console.WriteLine("SDL_OpenAudioDevice failed: " + SDL.SDL_GetError());
            return;
        }
        _sampleRate = obtained.freq;
    _format = obtained.format;
        _channels = obtained.channels;
        SDL.SDL_PauseAudioDevice(_device, 0);
    }

    public void Beep()
    {
        PlayTone(800, 200);
    }

    public void PlayTone(int frequencyHz, int durationMs)
    {
        if (_device == 0) return;
        if (frequencyHz <= 0 || durationMs <= 0) return;
        int sampleCount = (int)(_sampleRate * (durationMs / 1000.0));
        var buffer = new float[sampleCount];
        double phase = 0;
        double phaseInc = 2.0 * Math.PI * frequencyHz / _sampleRate;
        for (int i = 0; i < sampleCount; i++)
        {
            // Simple square wave
            double s = Math.Sin(phase);
            buffer[i] = s >= 0 ? 0.2f : -0.2f;
            phase += phaseInc;
        }
        int byteLen = buffer.Length * sizeof(float);
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            SDL.SDL_QueueAudio(_device, handle.AddrOfPinnedObject(), (uint)byteLen);
        }
        finally
        {
            handle.Free();
        }
    }

    public void PlayMusicString(string musicString)
    {
        // Minimal: treat as a single beep for now
        Beep();
    }

    public void Dispose()
    {
        if (_device != 0)
        {
            SDL.SDL_CloseAudioDevice(_device);
            _device = 0;
        }
    }
}
