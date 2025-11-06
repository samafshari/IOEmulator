using System.Windows;

namespace Neat.UI;

public partial class App : Application
{
    public App()
    {
        this.Startup += OnStartup;
        InitializeComponent();
    }

    private void OnStartup(object sender, StartupEventArgs e)
    {
        this.RootVisual = new MainPage();
    }
}
