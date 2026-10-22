using Microsoft.UI.Xaml;
using PlateWatch.App.Services;

namespace PlateWatch.App;

public partial class App : Application
{
    private DemoHost? _host;
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host = new DemoHost();
        _host.Start();
        // M2 demo: open the full window; the tray icon + popup arrive in the next slice.
        _window = new MainWindow(_host.Fleet);
        _window.Activate();
    }
}
