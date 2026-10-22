using Microsoft.UI.Xaml;
using PlateWatch.App.Services;

namespace PlateWatch.App;

public partial class App : Application
{
    private DemoHost? _host;
    private TrayService? _tray;
    private ToastService? _toasts;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settings = SettingsStore.Load();
        _host = new DemoHost();
        _host.Start();
        _tray = new TrayService(_host.Fleet, settings);
        _toasts = new ToastService(_host.Fleet.Core, settings, _tray.ShowWindow);
        // first launch shows the full window; afterwards it lives in the tray
        _tray.ShowWindow();
    }
}
