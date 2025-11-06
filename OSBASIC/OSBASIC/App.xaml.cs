using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OSBASIC
{
    public sealed partial class App : Application
    {
        public static MainPage CurrentMainPage { get; private set; }
        public App()
        {
            this.InitializeComponent();

            // Enter construction logic here...
            // For WASM, load the emulator UI page (QBPage). Keep CurrentMainPage available for hosts expecting it.
            try
            {
                var qb = new QBPage();
                Window.Current.Content = qb;
            }
            catch
            {
                // Fallback to minimal MainPage if QBPage fails
                var mainPage = new MainPage();
                CurrentMainPage = mainPage;
                Window.Current.Content = mainPage;
                return;
            }

            // Also initialize a stub MainPage instance for hosts that query this property (may be unused on WASM)
            try { CurrentMainPage = new MainPage(); } catch { CurrentMainPage = null; }
        }
    }
}
