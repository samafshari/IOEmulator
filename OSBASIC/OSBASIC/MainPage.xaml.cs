using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace OSBASIC
{
    // Minimal stub page used to validate OpenSilver rendering in WASM.
    // Exposes the same surface API as the old MainPage so the Browser host compiles and runs.
    public sealed partial class MainPage : Page
    {
        // Events expected by the host (no-op in this hello-world page)
        public event Action<char> PrintableChar;
        public event Action<Key, bool, bool, bool, bool> SpecialKey;
        public event Action<int, int, bool, bool, bool> MouseStateChanged;
        public event Action<string> SampleSelected;
        public event Action<double> SpeedChanged;
        public event Action<int> RefreshChanged;

        public MainPage()
        {
            this.InitializeComponent();
        }

        public void SetRefreshInterval(int intervalMs) { /* no-op */ }
        public void SetFrameSize(int width, int height) { /* no-op */ }
        public void UpdateFrame(int[] argbPixels) { /* no-op */ }
        public void SetSamplesList(string[] names) { /* no-op */ }
    }
}
