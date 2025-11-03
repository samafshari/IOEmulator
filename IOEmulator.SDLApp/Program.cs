using System;
using System.Runtime.InteropServices;
using System.Threading;
using SDL2;
using Neat;

namespace Neat.SDLApp;

internal static class Program
{
    private static IntPtr _window;
    private static IntPtr _renderer;
    private static IntPtr _texture;
    private static int _scale = 3;

    private static IOEmulator _io = new IOEmulator();
    private static QBasicApi _qb = new QBasicApi(_io);

    private static byte[] _rgba = Array.Empty<byte>();

    [STAThread]
    public static int Main(string[] args)
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_AUDIO) < 0)
        {
            Console.WriteLine("SDL_Init Error: " + SDL.SDL_GetError());
            return 1;
        }

        // If a sample name is provided, run it via the interpreter in the background
        string? sample = args != null && args.Length > 0 ? args[0] : "HELLO";
        try
        {
            var src = QBasicSamples.Load(sample);
            var interp = new QBasicInterpreter(_qb);
            _ = System.Threading.Tasks.Task.Run(() => interp.Run(src));
        }
        catch (Exception ex)
        {
            _qb.SCREEN(13);
            _qb.COLOR(15, 1);
            _qb.CLS();
            _qb.PRINT($"Failed to load sample '{sample}': {ex.Message}");
        }

        int w = _io.ResolutionW;
        int h = _io.ResolutionH;
        _window = SDL.SDL_CreateWindow("IOEmulator SDL",
            SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
            w * _scale, h * _scale,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
        if (_window == IntPtr.Zero)
        {
            Console.WriteLine("SDL_CreateWindow Error: " + SDL.SDL_GetError());
            return 1;
        }

        _renderer = SDL.SDL_CreateRenderer(_window, -1,
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (_renderer == IntPtr.Zero)
        {
            Console.WriteLine("SDL_CreateRenderer Error: " + SDL.SDL_GetError());
            return 1;
        }

        _texture = SDL.SDL_CreateTexture(_renderer,
            SDL.SDL_PIXELFORMAT_ABGR8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            w, h);
        if (_texture == IntPtr.Zero)
        {
            Console.WriteLine("SDL_CreateTexture Error: " + SDL.SDL_GetError());
            return 1;
        }

        SDL.SDL_StartTextInput();

        bool running = true;
        var ev = new SDL.SDL_Event();

    // If no sample, still draw something (sample will overwrite anyway)
    _io.Line(0, 0, w-1, h-1, 12);
    _io.Line(0, h-1, w-1, 0, 10);

        while (running)
        {
            while (SDL.SDL_PollEvent(out ev) == 1)
            {
                switch (ev.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        running = false; break;
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        HandleKeyDown(ev.key.keysym.sym, ev.key.keysym.mod);
                        break;
                    case SDL.SDL_EventType.SDL_TEXTINPUT:
                        unsafe
                        {
                            var text = Marshal.PtrToStringUTF8((IntPtr)ev.text.text);
                            if (!string.IsNullOrEmpty(text))
                            {
                                foreach (var ch in text)
                                {
                                    _io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, ch));
                                    _io.PutChar(ch);
                                }
                            }
                        }
                        break;
                }
            }

            // Upload IOEmulator VRAM
            UploadTexture();

            SDL.SDL_RenderClear(_renderer);
            var dst = new SDL.SDL_Rect { x = 0, y = 0, w = w * _scale, h = h * _scale };
            SDL.SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, ref dst);
            SDL.SDL_RenderPresent(_renderer);
        }

        SDL.SDL_StopTextInput();
        if (_texture != IntPtr.Zero) SDL.SDL_DestroyTexture(_texture);
        if (_renderer != IntPtr.Zero) SDL.SDL_DestroyRenderer(_renderer);
        if (_window != IntPtr.Zero) SDL.SDL_DestroyWindow(_window);
        SDL.SDL_Quit();
        return 0;
    }

    private static void UploadTexture()
    {
        int w = _io.ResolutionW;
        int h = _io.ResolutionH;
        int pitch = 0;
        IntPtr pixels;
        if (SDL.SDL_LockTexture(_texture, IntPtr.Zero, out pixels, out pitch) != 0)
            return;
        try
        {
            int needed = w * h * 4;
            if (_rgba.Length != needed)
                _rgba = new byte[needed];
            int di = 0;
            var src = _io.PixelBuffer;
            for (int i = 0; i < src.Length; i++)
            {
                var c = src[i];
                // ABGR8888: A, B, G, R order in memory for the chosen format
                _rgba[di++] = 255;   // A
                _rgba[di++] = c.B;   // B
                _rgba[di++] = c.G;   // G
                _rgba[di++] = c.R;   // R
            }
            // Copy per row respecting pitch
            unsafe
            {
                byte* dst = (byte*)pixels;
                fixed (byte* srcPtr = _rgba)
                {
                    int srcStride = w * 4;
                    for (int y = 0; y < h; y++)
                    {
                        Buffer.MemoryCopy(srcPtr + y * srcStride, dst + y * pitch, pitch, srcStride);
                    }
                }
            }
        }
        finally
        {
            SDL.SDL_UnlockTexture(_texture);
        }
    }

    private static void HandleKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
    {
        switch (key)
        {
            case SDL.SDL_Keycode.SDLK_ESCAPE:
                // Signal quit by injecting a specific key or handling in loop
                Environment.Exit(0);
                break;
            case SDL.SDL_Keycode.SDLK_RETURN:
                _io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Enter, '\n'));
                _io.PutChar('\n');
                break;
            case SDL.SDL_Keycode.SDLK_BACKSPACE:
                _io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Backspace));
                _io.PutChar(8);
                break;
            default:
                break;
        }
    }
}
