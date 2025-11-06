using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neat;

namespace OSBASIC.Simulator
{
    internal static class Host
    {
        private static IOEmulator _io;
        private static QBasicApi _qb;
        private static QBasicInterpreter _interp;
        private static CancellationTokenSource _cts;
        private static System.Timers.Timer _timer;
        private static double _speedFactor = 1.0;
        private static int _lastPixelCount = 0;
    // Use emulator's packed buffer directly; no conversion
        private static bool _initialized = false;

        public static void InitializeWhenReady()
        {
            if (_initialized) return;
            _initialized = true;
            // Poll on a background task until the OpenSilver page is ready
            Task.Run(async () =>
            {
                OSBASIC.MainPage page = null;
                for (int i = 0; i < 200; i++) // up to ~4s
                {
                    page = OSBASIC.App.CurrentMainPage;
                    if (page != null) break;
                    await Task.Delay(20);
                }
                if (page == null) return;

                // Switch to UI thread for UI interactions
                page.Dispatcher.BeginInvoke(() => Initialize(page));
            });
        }

        private static void Initialize(OSBASIC.MainPage page)
        {
            // Create emulator and QBASIC interpreter
            _io = new IOEmulator();
            _qb = new QBasicApi(_io, new SimulatorSoundDriver());

            // Wire input from OpenSilver page to emulator
            page.PrintableChar += ch => _io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, ch));
            page.SpecialKey += (key, shift, ctrl, alt, isDown) =>
            {
                var code = MapKeyName(key.ToString());
                _io.InjectKey(new KeyEvent(isDown ? KeyEventType.Down : KeyEventType.Up, code, null, shift, ctrl, alt));
            };
            page.MouseStateChanged += (x, y, left, right, middle) => _io.SetMouseState(x, y, left, right, middle);

            // Populate samples in UI
            var names = QBasicSamples.List().ToArray();
            page.SetSamplesList(names);

            // React to sample/speed/refresh events
            page.SampleSelected += name => StartProgram(page, name);
            page.SpeedChanged += val => { _speedFactor = val; if (_interp != null) _interp.SpeedFactor = _speedFactor; };
            page.RefreshChanged += ms => { if (_timer != null) _timer.Interval = ms; };

            // Start default program
            StartProgram(page, "GUESS.bas");

            // Rendering loop
            _lastPixelCount = 0;
            _timer = new System.Timers.Timer(16);
            _timer.Elapsed += (s, e) =>
            {
                try
                {
                    if (_io.ResolutionW * _io.ResolutionH != _lastPixelCount)
                    {
                        _lastPixelCount = _io.ResolutionW * _io.ResolutionH;
                        var w = _io.ResolutionW; var h = _io.ResolutionH;
                        page.Dispatcher.BeginInvoke(() => page.SetFrameSize(w, h));
                    }
                    if (!_io.Dirty) return;
                    var frame = _io.PixelBuffer32;
                    page.Dispatcher.BeginInvoke(() => page.UpdateFrame(frame));
                    _io.ResetDirty();
                }
                catch { }
            };
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private static void StartProgram(OSBASIC.MainPage page, string name)
        {
            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }
            _cts = new CancellationTokenSource();
            _io.SetBufferingMode(false);
            var src = QBasicSamples.Load(name);
            _interp = new QBasicInterpreter(_qb) { SpeedFactor = _speedFactor };
            _ = Task.Run(() =>
            {
                try { _interp.Run(src, _cts.Token); }
                catch (OperationCanceledException) { }
                catch { }
            });
        }

        private static KeyCode MapKeyName(string key)
        {
            switch (key)
            {
                case "Enter": return KeyCode.Enter;
                case "Back": return KeyCode.Backspace;
                case "Delete": return KeyCode.Delete;
                case "Home": return KeyCode.Home;
                case "End": return KeyCode.End;
                case "Tab": return KeyCode.Tab;
                case "Escape": return KeyCode.Escape;
                case "Left": return KeyCode.Left;
                case "Right": return KeyCode.Right;
                case "Up": return KeyCode.Up;
                case "Down": return KeyCode.Down;
                default: return KeyCode.Unknown;
            }
        }

    }
}
