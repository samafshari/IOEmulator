using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

using OpenSilver.WebAssembly;

using System.Threading.Tasks;
using System.Threading;
using System;
using Neat;
using System.Linq;
using Microsoft.JSInterop;

namespace OSBASIC.Browser.Pages
{
    [Route("/")]
    public class Index : ComponentBase
    {
        [Inject] public IJSRuntime JS { get; set; }
    private bool _enableEmulator = true; // Toggle if you want to run UI without emulator
        private IOEmulator _io;
        private QBasicApi _qb;
        private QBasicInterpreter _interp;
        private CancellationTokenSource _cts;
        private CancellationTokenSource _renderCts;
        private double _speedFactor = 1.0;
        private int _refreshMs = 16;
        private static int _lastPixelCount = 0;
    // Using emulator's packed buffer directly; no per-frame conversion needed

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
        }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await JS.InvokeVoidAsync("console.log", "Starting OpenSilver app...");
            await Runner.RunApplicationAsync<OSBASIC.App>();

            // Get the OpenSilver page instance
            if (!_enableEmulator)
            {
                await JS.InvokeVoidAsync("console.log", "OpenSilver started (emulator disabled)");
                return;
            }

            // Retrieve the emulator page (QBPage) which is the Window content
            var pageContent = System.Windows.Window.Current.Content as OSBASIC.QBPage;
            if (pageContent == null)
            {
                await JS.InvokeVoidAsync("console.log", "QBPage not found; cannot initialize emulator");
                return;
            }
            await JS.InvokeVoidAsync("console.log", "QBPage found. Initializing emulator...");

                await JS.InvokeVoidAsync("console.log", "Initializing emulator...");
                // Create emulator and QBASIC interpreter in Browser host (can reference net8 libs)
                _io = new IOEmulator();
                _qb = new QBasicApi(_io, new WebAudioSoundDriver(JS));

                // Wire input from OpenSilver page to emulator
                pageContent.PrintableChar += ch => _io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, ch));
                pageContent.SpecialKey += (key, shift, ctrl, alt, isDown) =>
                {
                    var code = MapKey(key);
                    _io.InjectKey(new KeyEvent(isDown ? KeyEventType.Down : KeyEventType.Up, code, null, shift, ctrl, alt));
                };
                pageContent.MouseStateChanged += (x, y, left, right, middle) =>
                {
                    _io.SetMouseState(x, y, left, right, middle);
                };

                // Populate samples in UI
                var names = QBasicSamples.List().ToArray();
                pageContent.SetSamplesList(names);

                // React to sample/speed/refresh events
                pageContent.SampleSelected += name => StartProgram(pageContent, name);
                pageContent.SpeedChanged += val => { _speedFactor = val; if (_interp != null) _interp.SpeedFactor = _speedFactor; };
                pageContent.RefreshChanged += ms => { _refreshMs = ms; };

                // Start default program
                // Prefer a non-blocking sample in WASM; LINES.bas uses SLEEP 0 and runs continuously
                StartProgram(pageContent, "LINES.bas");

                // Rendering loop using Task.Delay for WASM compatibility
                _renderCts = new CancellationTokenSource();
                _refreshMs = 16;
                _ = RenderLoop(pageContent, _renderCts.Token);
                await JS.InvokeVoidAsync("console.log", "Initialization complete");
        }

        private void StartProgram(OSBASIC.QBPage page, string name)
        {
            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }
            _cts = new CancellationTokenSource();
            // Double buffering for smoother WASM rendering; host will swap once per frame
            _io.SetBufferingMode(true);
            var src = QBasicSamples.Load(name);
            _interp = new QBasicInterpreter(_qb) { SpeedFactor = _speedFactor };
            // Cooperative stepping for WASM: load program, then step in render loop
            try { _interp.LoadProgram(src); } catch { }
        }

        private async Task RenderLoop(OSBASIC.QBPage page, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Advance interpreter within a time budget per frame to keep UI responsive
                    if (_interp != null)
                    {
                        var start = DateTime.UtcNow;
                        // Compute budget scales with speed factor; cap to avoid starving UI
                        int computeBudgetMs = Math.Max(2, (int)Math.Round(_refreshMs * _speedFactor * 0.6));
                        while ((DateTime.UtcNow - start).TotalMilliseconds < computeBudgetMs)
                        {
                            try { if (!_interp.Step(2000, token)) break; }
                            catch (OperationCanceledException) { break; }
                            catch (Exception ex) { await JS.InvokeVoidAsync("console.log", $"QB error: {ex.Message}"); break; }
                        }
                    }

                    // Swap back buffer to front and mark dirty once per frame
                    _io.BufferSwap();
                    // Resize if needed
                    if (_io.ResolutionW * _io.ResolutionH != _lastPixelCount)
                    {
                        _lastPixelCount = _io.ResolutionW * _io.ResolutionH;
                        var w = _io.ResolutionW; var h = _io.ResolutionH;
                        page.SetFrameSize(w, h);
                    }
                // Always update after swap; for static scenes this is still cheap
                page.UpdateFrame(_io.PixelBuffer32);
                _io.ResetDirty();
                }
                catch (Exception ex)
                {
                    // Log to console for debugging
                    await JS.InvokeVoidAsync("console.log", $"Render error: {ex.Message}");
                }
                await Task.Delay(_refreshMs, token);
            }
        }

        private static KeyCode MapKey(System.Windows.Input.Key key)
        {
            switch (key)
            {
                case System.Windows.Input.Key.Enter: return KeyCode.Enter;
                case System.Windows.Input.Key.Back: return KeyCode.Backspace;
                case System.Windows.Input.Key.Delete: return KeyCode.Delete;
                case System.Windows.Input.Key.Home: return KeyCode.Home;
                case System.Windows.Input.Key.End: return KeyCode.End;
                case System.Windows.Input.Key.Tab: return KeyCode.Tab;
                case System.Windows.Input.Key.Escape: return KeyCode.Escape;
                case System.Windows.Input.Key.Left: return KeyCode.Left;
                case System.Windows.Input.Key.Right: return KeyCode.Right;
                case System.Windows.Input.Key.Up: return KeyCode.Up;
                case System.Windows.Input.Key.Down: return KeyCode.Down;
                default: return KeyCode.Unknown;
            }
        }

    }

    // Browser-side no-op sound; implement WebAudio later
    internal sealed class NoOpSoundDriver : ISoundDriver
    {
        public void Beep() { }
        public void PlayMusicString(string musicString) { }
        public void PlayTone(int frequencyHz, int durationMs) { }
    }
}