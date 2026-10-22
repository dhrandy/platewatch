using Microsoft.UI.Xaml;
using PlateWatch.App.Services;

namespace PlateWatch.App;

public partial class App : Application
{
    private DemoHost? _host;
    private TrayService? _tray;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host = new DemoHost();
        _host.Start();
        _tray = new TrayService(_host.Fleet);
        // first launch shows the full window; afterwards it lives in the tray
        _tray.ShowWindow();
    }
}
