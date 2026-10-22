using Microsoft.UI.Xaml;
using PlateWatch.App.Services;

namespace PlateWatch.App;

public partial class App : Application
{
    private DemoHost? _host;
    private PopupWindow? _popup;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host = new DemoHost();
        _host.Start();
        // M2 demo: open the popup directly; the tray icon arrives in the next slice.
        _popup = new PopupWindow(_host.Fleet.Printers[0]);
        _popup.ShowAtTray();
    }
}
