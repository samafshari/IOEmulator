using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OSBASIC
{
    public partial class MainPage : Page
    {
    private Image _image;
    private DispatcherTimer _timer;
    private WriteableBitmap _wbmp;
    private int _currentRefreshMs = 16; // ~60 FPS default
    private int _frameW = 0, _frameH = 0;
    private bool _leftDown = false, _rightDown = false, _middleDown = false;
    private TextBox _input;
    private bool _inputHasFocus = false;

    // Input and control bridging: expose events for host (Browser project) to handle
    public event System.Action<char> PrintableChar;
    public event System.Action<Key, bool, bool, bool, bool> SpecialKey; // (key, shift, ctrl, alt, isDown)
    public event System.Action<int, int, bool, bool, bool> MouseStateChanged; // (x, y, left, right, middle)
    public event System.Action<string> SampleSelected;
    public event System.Action<double> SpeedChanged;
    public event System.Action<int> RefreshChanged;

        public MainPage()
        {
            this.InitializeComponent();

            _image = this.FindName("VramImage") as Image;
            _input = this.FindName("InputCapture") as TextBox;
            if (_input != null)
            {
                _input.GotFocus += (s, e) => { _inputHasFocus = true; };
                _input.LostFocus += (s, e) => { _inputHasFocus = false; };
            }

            // Render timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(_currentRefreshMs);
            _timer.Tick += (s, e) => SafeTick();
            _timer.Start();

            // Input events
            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseRightButtonDown += OnMouseRightButtonDown;
            this.MouseRightButtonUp += OnMouseRightButtonUp;
            // Ensure focus for keyboard input when clicking surface
            if (_image != null)
            {
                _image.MouseLeftButtonDown += (s, e) => { if (_input != null) _input.Focus(); else this.Focus(); };
            }

            // Focus input capture initially
            try { if (_input != null) _input.Focus(); } catch { }

            // Default selections
            try
            {
                var speedCombo = this.FindName("SpeedCombo") as ComboBox;
                if (speedCombo != null && speedCombo.Items.Count > 0)
                {
                    speedCombo.SelectedIndex = 4; // 100%
                }
                var refreshCombo = this.FindName("RefreshCombo") as ComboBox;
                if (refreshCombo != null)
                {
                    // 60 FPS (16ms)
                    for (int i = 0; i < refreshCombo.Items.Count; i++)
                    {
                        var item = refreshCombo.Items[i] as ComboBoxItem;
                        if (item != null && item.Tag != null && item.Tag.ToString() == "16")
                        {
                            refreshCombo.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private void SafeTick()
        {
            try { /* host triggers rendering via UpdateFrame; keep timer for future UI updates if needed */ }
            catch { /* ignore per-frame render errors */ }
        }

        public void SetRefreshInterval(int intervalMs)
        {
            _currentRefreshMs = Math.Max(1, intervalMs);
            if (_timer != null)
            {
                _timer.Interval = TimeSpan.FromMilliseconds(_currentRefreshMs);
            }
        }

        // Host-driven rendering API
        public void SetFrameSize(int width, int height)
        {
            if (_wbmp == null || _wbmp.PixelWidth != width || _wbmp.PixelHeight != height)
            {
                _wbmp = new WriteableBitmap(width, height);
                if (_image != null) _image.Source = _wbmp;
            }
            _frameW = width;
            _frameH = height;
        }

        public void UpdateFrame(int[] argbPixels)
        {
            if (_wbmp == null) return;
            var pixels = _wbmp.Pixels;
            int count = System.Math.Min(pixels.Length, argbPixels.Length);
            for (int i = 0; i < count; i++) pixels[i] = argbPixels[i];
            _wbmp.Invalidate();
        }

        // Host calls to populate samples list
        public void SetSamplesList(string[] names)
        {
            var combo = this.FindName("SamplesCombo") as ComboBox;
            if (combo == null) return;
            combo.Items.Clear();
            foreach (var n in names)
            {
                combo.Items.Add(new ComboBoxItem { Content = n });
            }
            // Select default if present
            for (int i = 0; i < combo.Items.Count; i++)
            {
                var item = combo.Items[i] as ComboBoxItem;
                if (item != null && (string)item.Content == "GUESS.bas")
                {
                    combo.SelectedIndex = i;
                    break;
                }
            }
        }

        // UI handlers to raise events to host
        private void OnSampleChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null) return;
            var item = combo.SelectedItem as ComboBoxItem;
            if (item == null) return;
            var name = item.Content as string;
            var handler = SampleSelected; if (handler != null && name != null) handler(name);
        }

        private void OnSpeedChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null) return;
            var item = combo.SelectedItem as ComboBoxItem;
            if (item == null) return;
            double val = 1.0;
            if (item.Tag != null && double.TryParse(item.Tag.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
                val = d;
            var handler = SpeedChanged; if (handler != null) handler(val);
        }

        private void OnRefreshChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null) return;
            var item = combo.SelectedItem as ComboBoxItem;
            if (item == null) return;
            int ms = _currentRefreshMs;
            if (item.Tag != null)
            {
                int.TryParse(item.Tag.ToString(), out ms);
            }
            var handler = RefreshChanged; if (handler != null) handler(ms);
        }

        private static (bool shift, bool ctrl, bool alt) GetMods()
        {
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            return (shift, ctrl, alt);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var mods = GetMods();
            bool shift = mods.shift, ctrl = mods.ctrl, alt = mods.alt;
            // If hidden input is not focused, synthesize printable characters from key events
            if (!_inputHasFocus)
            {
                if (TryMapKeyToChar(e.Key, shift, out var ch))
                {
                    var chHandler = PrintableChar; if (chHandler != null) chHandler(ch);
                    e.Handled = true; return;
                }
            }
            // Otherwise, or for non-printables, signal special keys
            var handler = SpecialKey; if (handler != null) handler(e.Key, shift, ctrl, alt, true);
            e.Handled = true;
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            var mods = GetMods();
            bool shift = mods.shift, ctrl = mods.ctrl, alt = mods.alt;
            var handler = SpecialKey; if (handler != null) handler(e.Key, shift, ctrl, alt, false);
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_image == null) return;
            var pos = e.GetPosition(_image);
            var (x, y) = MapPointToEmu(pos.X, pos.Y, _image);
            var handler = MouseStateChanged; if (handler != null) handler(x, y, _leftDown, _rightDown, _middleDown);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_image == null) return;
            var pos = e.GetPosition(_image);
            var (x, y) = MapPointToEmu(pos.X, pos.Y, _image);
            _leftDown = true;
            var handler = MouseStateChanged; if (handler != null) handler(x, y, _leftDown, _rightDown, _middleDown);
            this.Focus();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_image == null) return;
            var pos = e.GetPosition(_image);
            var (x, y) = MapPointToEmu(pos.X, pos.Y, _image);
            _leftDown = false;
            var handler = MouseStateChanged; if (handler != null) handler(x, y, _leftDown, _rightDown, _middleDown);

        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_image == null) return;
            var pos = e.GetPosition(_image);
            var (x, y) = MapPointToEmu(pos.X, pos.Y, _image);
            _rightDown = true;
            var handler = MouseStateChanged; if (handler != null) handler(x, y, _leftDown, _rightDown, _middleDown);
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_image == null) return;
            var pos = e.GetPosition(_image);
            var (x, y) = MapPointToEmu(pos.X, pos.Y, _image);
            _rightDown = false;
            var handler = MouseStateChanged; if (handler != null) handler(x, y, _leftDown, _rightDown, _middleDown);
        }

        private (int x, int y) MapPointToEmu(double px, double py, FrameworkElement target)
        {
            int w = _frameW, h = _frameH;
            double iw = target.ActualWidth, ih = target.ActualHeight;
            if (iw <= 0 || ih <= 0 || w <= 0 || h <= 0) return (0, 0);
            double scale = Math.Min(iw / w, ih / h);
            double vw = w * scale, vh = h * scale;
            double ox = (iw - vw) / 2.0, oy = (ih - vh) / 2.0;
            double x = Math.Max(0, Math.Min(w - 1, (px - ox) / scale));
            double y = Math.Max(0, Math.Min(h - 1, (py - oy) / scale));
            return ((int)x, (int)y);
        }

        private static bool TryMapKeyToChar(Key key, bool shift, out char ch)
        {
            ch = '\0';
            // Letters
            if (key >= Key.A && key <= Key.Z)
            {
                int offset = (int)(key - Key.A);
                ch = (char)((shift ? 'A' : 'a') + offset);
                return true;
            }
            // Digits row
            if (key >= Key.D0 && key <= Key.D9)
            {
                int d = (int)(key - Key.D0);
                if (!shift) { ch = (char)('0' + d); return true; }
                switch (d)
                {
                    case 0: ch = ')'; return true;
                    case 1: ch = '!'; return true;
                    case 2: ch = '@'; return true;
                    case 3: ch = '#'; return true;
                    case 4: ch = '$'; return true;
                    case 5: ch = '%'; return true;
                    case 6: ch = '^'; return true;
                    case 7: ch = '&'; return true;
                    case 8: ch = '*'; return true;
                    case 9: ch = '(' ; return true;
                    default: return false;
                }
            }
            // Numpad digits (no shift variants)
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                ch = (char)('0' + (int)(key - Key.NumPad0));
                return true;
            }
            if (key == Key.Space) { ch = ' '; return true; }
            return false;
        }

        private void OnInputTextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            var text = tb.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var handler = PrintableChar;
                if (handler != null)
                {
                    foreach (var c in text)
                    {
                        handler(c);
                    }
                }
                // Clear to avoid echo
                try { tb.Text = string.Empty; } catch { }
            }
        }
    }
}
