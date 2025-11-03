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
    private Image? _image;
    private readonly DispatcherTimer? _timer;
    private WriteableBitmap? _wbmp;
    private bool _loggedOnce = false;
    private MenuItem? _samplesMenu;
    private string _currentSample = "GUESS";

    public MainWindow()
    {
        InitializeComponent();

        _qb = new QBasicApi(_io, new AvaloniaSoundDriver());

        // Find UI elements
        _image = this.FindControl<Image>("vramImage");
        _samplesMenu = this.FindControl<MenuItem>("samplesMenu");
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

        // Run a default BASIC sample in the background
        StartSample(_currentSample);
        

    // Refresh at ~60 FPS on UI thread at render priority
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, __) => SafeTick());
        _timer.Start();

        // Hook text input for printable characters
    this.AddHandler(TextInputEvent, OnTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        this.KeyDown += OnKeyDown;
    }

    private void PopulateSamplesMenu()
    {
        if (_samplesMenu == null) return;
    _samplesMenu.ItemsSource = null; // reset
    var items = new System.Collections.Generic.List<MenuItem>();
        foreach (var res in QBasicSamples.List().OrderBy(n => n))
        {
            var name = res.EndsWith(".bas", StringComparison.OrdinalIgnoreCase) ? res[..^4] : res;
            var mi = new MenuItem { Header = name };
            mi.Click += (_, __) => StartSample(name);
            items.Add(mi);
        }
    _samplesMenu.ItemsSource = items;
    }

    private void StartSample(string sample)
    {
        _currentSample = sample;
        // cancel existing
        try { _runCts?.Cancel(); } catch { }
        _runCts?.Dispose();
        _runCts = new System.Threading.CancellationTokenSource();
        try
        {
            var src = QBasicSamples.Load(sample);
            var interp = new QBasicInterpreter(_qb);
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try { interp.Run(src, _runCts.Token); }
                catch (System.OperationCanceledException) { /* normal on close */ }
                catch { /* swallow to avoid unobserved exceptions in background */ }
            });
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
            if (_wbmp == null || _wbmp.PixelSize.Width != w || _wbmp.PixelSize.Height != h)
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
        _io.InjectKey(new KeyEvent(KeyEventType.Down, code));
        // Do not echo special keys here; leave output behavior to the interpreter/app.
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
