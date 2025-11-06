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

            var mainPage = new MainPage();
            CurrentMainPage = mainPage;
            Window.Current.Content = mainPage;
        }
    }
}
