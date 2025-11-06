using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Neat;

namespace Neat.UI;

public partial class MainPage : UserControl
{
    private readonly IOEmulator _io = new IOEmulator();
    private readonly QBasicApi _qb;
    private QBasicInterpreter? _interp;
    private System.Threading.CancellationTokenSource? _runCts;
    private DispatcherTimer? _timer;
    private WriteableBitmap? _wbmp;
    private string _currentSample = "GUESS";
    private double _currentSpeed = 1.0;

    public MainPage()
    {
        InitializeComponent();
        _qb = new QBasicApi(_io, new WebSoundDriver());
        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        MouseMove += OnMouseMove;
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Populate samples
        var samples = QBasicSamples.List().OrderBy(n => n).ToArray();
        SamplesCombo.ItemsSource = samples;
        SamplesCombo.SelectedItem = samples.FirstOrDefault(s => s.Equals(_currentSample + ".bas", StringComparison.OrdinalIgnoreCase)) ?? samples.FirstOrDefault();
        SamplesCombo.SelectionChanged += (_, __) =>
        {
            var name = (SamplesCombo.SelectedItem as string) ?? _currentSample;
            if (name.EndsWith(".bas", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
            StartSample(name);
        };

        // Populate speed options
        var speeds = new (double val, string label)[]
        {
            (0.5, "50%"), (0.75, "75%"), (1.0, "100%"), (1.5, "150%"), (2.0, "200%"), (3.0, "300%")
        };
        SpeedCombo.ItemsSource = speeds.Select(s => s.label).ToArray();
        SpeedCombo.SelectedIndex = 2;
        SpeedCombo.SelectionChanged += (_, __) =>
        {
            var idx = SpeedCombo.SelectedIndex;
            if (idx >= 0 && idx < speeds.Length)
            {
                _currentSpeed = speeds[idx].val;
                if (_interp != null) _interp.SpeedFactor = _currentSpeed;
            }
        };

        // Start default sample
        StartSample(_currentSample);

        // Render timer (~60 FPS)
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, __) => SafeTick();
        _timer.Start();
    }

    private void StartSample(string sample)
    {
        _currentSample = sample;
        try { _runCts?.Cancel(); } catch { }
        _runCts?.Dispose();
        _runCts = new System.Threading.CancellationTokenSource();
        try
        {
            var src = QBasicSamples.Load(sample);
            var interp = new QBasicInterpreter(_qb) { SpeedFactor = _currentSpeed };
            _interp = interp;
            // Run in background
            System.Threading.Tasks.Task.Run(() =>
            {
                try { interp.Run(src, _runCts.Token); }
                catch { }
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

    private void SafeTick()
    {
        try
        {
            int w = _io.ResolutionW, h = _io.ResolutionH;
            if (_wbmp == null || _wbmp.PixelWidth != w || _wbmp.PixelHeight != h)
            {
                _wbmp = new WriteableBitmap(w, h);
                VramImage.Source = _wbmp;
            }
            if (!_io.Dirty && _wbmp != null) return;
            if (_wbmp == null) return;
            // Silverlight-style WriteableBitmap exposes ARGB int[] Pixels with Invalidate()
            int expected = w * h;
            var src = _io.PixelBuffer;
            var px = _wbmp.Pixels;
            if (px.Length != expected) px = new int[expected];
            int count = Math.Min(expected, src.Length);
            for (int i = 0; i < count; i++)
            {
                var c = src[i];
                // ARGB: 0xAARRGGBB
                px[i] = (255 << 24) | (c.R << 16) | (c.G << 8) | (c.B);
            }
            if (count < expected)
            {
                var bg = _io.GetColor(_io.BackgroundColorIndex);
                int argb = (255 << 24) | (bg.R << 16) | (bg.G << 8) | (bg.B);
                for (int i = count; i < expected; i++) px[i] = argb;
            }
            _wbmp.Invalidate();
            _io.ResetDirty();
        }
        catch { }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape || (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
        {
            try { _runCts?.Cancel(); } catch { }
            e.Handled = true; return;
        }
        var code = MapKey(e.Key);
        _io.InjectKey(new KeyEvent(KeyEventType.Down, code, null,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)));
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        var code = MapKey(e.Key);
        _io.InjectKey(new KeyEvent(KeyEventType.Up, code, null,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)));
    }

    private KeyCode MapKey(Key key) => key switch
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
        _ => KeyCode.Unknown,
    };

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(VramImage);
        MapAndSetMouse(p);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(VramImage);
        MapAndSetMouse(p);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(VramImage);
        MapAndSetMouse(p);
    }

    private void MapAndSetMouse(Point pos)
    {
        int w = _io.ResolutionW, h = _io.ResolutionH;
        if (w <= 0 || h <= 0) return;
        // Assume image is Uniform stretch filling its container
        var img = VramImage;
        double iw = img.ActualWidth, ih = img.ActualHeight;
        double scale = Math.Min(iw / w, ih / h);
        double vw = w * scale, vh = h * scale;
        double ox = (iw - vw) / 2.0, oy = (ih - vh) / 2.0;
        double px = Math.Clamp((pos.X - ox) / scale, 0, w - 1);
        double py = Math.Clamp((pos.Y - oy) / scale, 0, h - 1);
        // Buttons state is not directly in pos; keep simple: only left for now
        _io.SetMouseState((int)px, (int)py, true, false, false);
    }
}
