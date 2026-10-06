using System.Configuration;
using System.Data;
using System.Windows;

namespace Tracker.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly TimeSpan _splashScreenDuration = TimeSpan.FromSeconds(3);

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        var splashScreen = new SplashWindow();

        splashScreen.Show();

        await Task.Delay(_splashScreenDuration);
        splashScreen.Close();

        mainWindow.Show();
    }
}

