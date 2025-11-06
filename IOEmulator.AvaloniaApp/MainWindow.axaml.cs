using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Media;
using Neat;
namespace Neat.UI;

public partial class MainWindow : Window
{
    private readonly IOEmulator _io = new IOEmulator();
    private readonly QBasicApi _qb;
    private System.Threading.CancellationTokenSource? _runCts;
    private QBasicInterpreter? _currentInterp;
    private Image? _image;
    private DispatcherTimer? _timer;
    private WriteableBitmap? _wbmp;
    private bool _loggedOnce = false;
    private MenuItem? _samplesMenu;
    private MenuItem? _speedMenu;
    private MenuItem? _refreshMenu;
    private string _currentSample = "GUESS";
    private double _currentSpeed = 1.0;
    private int _currentRefreshMs = 16; // ~60 FPS by default

    public MainWindow()
    {
        InitializeComponent();

        _qb = new QBasicApi(_io, new AvaloniaSoundDriver());

        // Find UI elements
        _image = this.FindControl<Image>("vramImage");
        _samplesMenu = this.FindControl<MenuItem>("samplesMenu");
    _speedMenu = this.FindControl<MenuItem>("speedMenu");
    _refreshMenu = this.FindControl<MenuItem>("refreshMenu");
        if (_image != null)
        {
            RenderOptions.SetBitmapInterpolationMode(_image, BitmapInterpolationMode.None);
        }

        // Populate Samples menu
        try
        {
            PopulateSamplesMenu();
        }
        catch { /* ignore menu population issues */ }

        // Populate Speed menu
        try
        {
            PopulateSpeedMenu();
        }
        catch { /* ignore menu population issues */ }

        // Populate Refresh menu
        try
        {
            PopulateRefreshMenu();
        }
        catch { /* ignore menu population issues */ }

        // Run a default BASIC sample in the background
        StartSample(_currentSample);
        

    // Refresh at adjustable FPS on UI thread at render priority
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(_currentRefreshMs), DispatcherPriority.Render, (_, __) => SafeTick());
        _timer.Start();

        // Hook text input for printable characters
    this.AddHandler(TextInputEvent, OnTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        this.KeyDown += OnKeyDown;
        this.KeyUp += OnKeyUp;
        // Mouse input
        this.PointerMoved += OnPointerMoved;
        this.PointerPressed += OnPointerPressed;
        this.PointerReleased += OnPointerReleased;
    }

    private void PopulateSamplesMenu()
    {
        if (_samplesMenu == null) return;
    _samplesMenu.ItemsSource = null; // reset
    var items = new System.Collections.Generic.List<MenuItem>();
        foreach (var res in QBasicSamples.List().OrderBy(n => n))
        {
            var name = res.EndsWith(".bas", StringComparison.OrdinalIgnoreCase) ? res[..^4] : res;
            var mi = new MenuItem { Header = name, Focusable = false };
            mi.Click += (_, __) => StartSample(name);
            items.Add(mi);
        }
    _samplesMenu.ItemsSource = items;
    }

    private void PopulateSpeedMenu()
    {
        if (_speedMenu == null) return;
        _speedMenu.ItemsSource = null;
        var items = new System.Collections.Generic.List<MenuItem>();
        
        var speeds = new[]
        {
            (0.1, "10% (Very Slow)"),
            (0.25, "25% (Slow)"),
            (0.5, "50% (Half Speed)"),
            (0.75, "75%"),
            (1.0, "100% (Normal)"),
            (1.5, "150%"),
            (2.0, "200% (2x)"),
            (3.0, "300% (3x)"),
            (5.0, "500% (5x)"),
            (10.0, "1000% (10x)"),
            (20.0, "2000% (20x)"),
            (50.0, "5000% (50x)"),
            (100.0, "10000% (100x - Maximum)")
        };

        foreach (var (speed, label) in speeds)
        {
            var mi = new MenuItem { Header = label, Focusable = false };
            double capturedSpeed = speed;
            mi.Click += (_, __) => SetSpeed(capturedSpeed);
            items.Add(mi);
        }
        _speedMenu.ItemsSource = items;
    }

    private void PopulateRefreshMenu()
    {
        if (_refreshMenu == null) return;
        _refreshMenu.ItemsSource = null;
        var items = new System.Collections.Generic.List<MenuItem>();
        // label -> interval ms
        var rates = new (int ms, string label)[]
        {
            (66, "15 FPS"),
            (33, "30 FPS"),
            (25, "40 FPS"),
            (16, "60 FPS (Default)"),
            (12, "80 FPS"),
            (8,  "120 FPS")
        };
        foreach (var (ms, label) in rates)
        {
            var mi = new MenuItem { Header = label, Focusable = false };
            int captured = ms;
            mi.Click += (_, __) => SetRefreshInterval(captured);
            items.Add(mi);
        }
        _refreshMenu.ItemsSource = items;
    }

    private void SetRefreshInterval(int intervalMs)
    {
        _currentRefreshMs = Math.Max(1, intervalMs);
        if (_timer != null)
        {
            try { _timer.Stop(); } catch { }
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(_currentRefreshMs), DispatcherPriority.Render, (_, __) => SafeTick());
            _timer.Start();
        }
    }

    private void SetSpeed(double speed)
    {
        _currentSpeed = speed;
        if (_currentInterp != null)
        {
            _currentInterp.SpeedFactor = speed;
        }
    }

    private void StartSample(string sample)
    {
        _currentSample = sample;
        // cancel existing
        try { _runCts?.Cancel(); } catch { }
        _runCts?.Dispose();
        _runCts = new System.Threading.CancellationTokenSource();
        // Reset buffering to default (single) when changing program
        try { _io.SetBufferingMode(false); } catch { }
        try
        {
            var src = QBasicSamples.Load(sample);
            var interp = new QBasicInterpreter(_qb)
            {
                SpeedFactor = _currentSpeed
            };
            _currentInterp = interp;
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try { interp.Run(src, _runCts.Token); }
                catch (System.OperationCanceledException) { /* normal on close */ }
                catch { /* swallow to avoid unobserved exceptions in background */ }
                finally
                {
                    // Also ensure buffer mode resets after program finishes,
                    // but only if this is still the active interpreter (avoid race with a new run)
                    try { if (ReferenceEquals(_currentInterp, interp)) _io.SetBufferingMode(false); } catch { }
                }
            });
            // Ensure the render surface has keyboard focus after menu selection
            try { _image?.Focus(); } catch { }
        }
        catch (Exception ex)
        {
            _qb.SCREEN(13);
            _qb.COLOR(15, 1);
            _qb.CLS();
            _qb.PRINT($"Failed to load sample '{sample}': {ex.Message}");
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // Proactively focus the render image so arrow keys work without clicking first
        try { _image?.Focus(); } catch { }
        SafeTick();
    }

    private void SafeTick()
    {
        try
        {
            // Ensure bitmap
            var w = _io.ResolutionW;
            var h = _io.ResolutionH;
            // One-time diagnostic to verify dimensions and font glyph sizes
            if (!_loggedOnce)
            {
                try
                {
                    var g = _io.GetGlyphForCharacter('A');
                    System.Diagnostics.Debug.WriteLine($"IOEmu: res={w}x{h} text={_io.TextCols}x{_io.TextRows} glyphA={g.Width}x{g.Height} pixels={_io.PixelBuffer.Length}");
                }
                catch { /* ignore diagnostics failures */ }
                _loggedOnce = true;
            }
            bool sizeChanged = _wbmp == null || _wbmp.PixelSize.Width != w || _wbmp.PixelSize.Height != h;
            if (sizeChanged)
            {
                _wbmp?.Dispose();
                // Use BGRA8888 which is the most widely supported format in Avalonia/Skia
                _wbmp = new WriteableBitmap(
                    new PixelSize(w, h),
                    new Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul);
                if (_image != null) _image.Source = _wbmp;
            }
            if (_wbmp == null) return;
            // Skip redraw if nothing changed and size is stable
            if (!sizeChanged && !_io.Dirty) return;
            using var fb = _wbmp.Lock();
            // Copy VRAM RGB to BGRA into surface, honoring framebuffer stride and avoiding races
            var srcBuf = _io.PixelBuffer; // snapshot reference
            int expectedPixels = w * h;
            var bgra = System.Buffers.ArrayPool<byte>.Shared.Rent(expectedPixels * 4);
            try
            {
                int di = 0;
                int pixelCount = Math.Min(srcBuf.Length, expectedPixels);
                for (int i = 0; i < pixelCount; i++)
                {
                    var c = srcBuf[i];
                    // BGRA ordering for PixelFormat.Bgra8888
                    bgra[di++] = c.B; bgra[di++] = c.G; bgra[di++] = c.R; bgra[di++] = 255;
                }
                if (pixelCount < expectedPixels)
                {
                    var bg = _io.GetColor(_io.BackgroundColorIndex);
                    for (int i = pixelCount; i < expectedPixels; i++)
                    {
                        bgra[di++] = bg.B; bgra[di++] = bg.G; bgra[di++] = bg.R; bgra[di++] = 255;
                    }
                }
                int srcStride = w * 4; // 4 bytes per pixel (BGRA)
                int dstStride = fb.RowBytes;
                var addr = fb.Address;
                for (int y = 0; y < h; y++)
                {
                    System.Runtime.InteropServices.Marshal.Copy(bgra, y * srcStride, addr + y * dstStride, srcStride);
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(bgra);
            }
            _image?.InvalidateVisual();
            _io.ResetDirty();
        }
        catch
        {
            // Swallow render-time exceptions to avoid breaking the render loop; we'll draw next tick
        }
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text))
        {
            foreach (var ch in e.Text)
            {
                // Inject as input only; do not echo here to avoid double-printing.
                _io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, ch));
            }
            e.Handled = true;
        }
    }

    private static (bool shift, bool ctrl, bool alt) GetMods(KeyEventArgs e)
        => (e.KeyModifiers.HasFlag(KeyModifiers.Shift), e.KeyModifiers.HasFlag(KeyModifiers.Control), e.KeyModifiers.HasFlag(KeyModifiers.Alt));

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Map a few special keys
        KeyCode code = e.Key switch
        {
            Key.Enter => KeyCode.Enter,
            Key.Back => KeyCode.Backspace,
            Key.Delete => KeyCode.Delete,
            Key.Home => KeyCode.Home,
            Key.End => KeyCode.End,
            Key.Tab => KeyCode.Tab,
            Key.Escape => KeyCode.Escape,
            Key.Left => KeyCode.Left,
            Key.Right => KeyCode.Right,
            Key.Up => KeyCode.Up,
            Key.Down => KeyCode.Down,
            _ => KeyCode.Unknown
        };
        var (shift, ctrl, alt) = GetMods(e);
        _io.InjectKey(new KeyEvent(KeyEventType.Down, code, null, shift, ctrl, alt));
        // Do not echo special keys here; leave output behavior to the interpreter/app.
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        KeyCode code = e.Key switch
        {
            Key.Enter => KeyCode.Enter,
            Key.Back => KeyCode.Backspace,
            Key.Delete => KeyCode.Delete,
            Key.Home => KeyCode.Home,
            Key.End => KeyCode.End,
            Key.Tab => KeyCode.Tab,
            Key.Escape => KeyCode.Escape,
            Key.Left => KeyCode.Left,
            Key.Right => KeyCode.Right,
            Key.Up => KeyCode.Up,
            Key.Down => KeyCode.Down,
            _ => KeyCode.Unknown
        };
        var (shift, ctrl, alt) = GetMods(e);
        _io.InjectKey(new KeyEvent(KeyEventType.Up, code, null, shift, ctrl, alt));
    }

    private (int x, int y) MapPointerToEmu(Point pos, Control target)
    {
        // Map pointer pos within Image with Stretch=Uniform to emulator pixel coords
        int w = _io.ResolutionW, h = _io.ResolutionH;
        double iw = target.Bounds.Width, ih = target.Bounds.Height;
        double scale = Math.Min(iw / w, ih / h);
        double vw = w * scale, vh = h * scale;
        double ox = (iw - vw) / 2.0, oy = (ih - vh) / 2.0;
        double px = Math.Clamp((pos.X - ox) / scale, 0, w - 1);
        double py = Math.Clamp((pos.Y - oy) / scale, 0, h - 1);
        return ((int)px, (int)py);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_image == null) return;
        var p = e.GetPosition(_image);
        var (x, y) = MapPointerToEmu(p, _image);
        var props = e.GetCurrentPoint(_image).Properties;
        _io.SetMouseState(x, y, props.IsLeftButtonPressed, props.IsRightButtonPressed, props.IsMiddleButtonPressed);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_image == null) return;
        var p = e.GetPosition(_image);
        var (x, y) = MapPointerToEmu(p, _image);
        var props = e.GetCurrentPoint(_image).Properties;
        _io.SetMouseState(x, y, props.IsLeftButtonPressed, props.IsRightButtonPressed, props.IsMiddleButtonPressed);
        // Clicking the surface should also ensure keyboard focus for subsequent key events
        try { _image?.Focus(); } catch { }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_image == null) return;
        var p = e.GetPosition(_image);
        var (x, y) = MapPointerToEmu(p, _image);
        var props = e.GetCurrentPoint(_image).Properties;
        _io.SetMouseState(x, y, props.IsLeftButtonPressed, props.IsRightButtonPressed, props.IsMiddleButtonPressed);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _runCts?.Cancel();
        _runCts?.Dispose();
        _timer?.Stop();
        _wbmp?.Dispose();
    }
}
