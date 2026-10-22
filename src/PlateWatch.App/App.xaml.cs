using Microsoft.UI.Xaml;
using PlateWatch.App.Services;
using PlateWatch.Core;

namespace PlateWatch.App;

public partial class App : Application
{
    private DemoHost? _host;
    private TrayService? _tray;
    private ToastService? _toasts;
    private List<LanBambuClient>? _realClients;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settings = SettingsStore.Load();
        Fleet coreFleet;
        if (settings.Settings.Printers.Count > 0)
        {
            _realClients = settings.Settings.Printers.Select(p => new LanBambuClient(
                new PrinterIdentity(p.Name, p.Model, p.Serial, p.IpAddress, Firmware: ""),
                string.IsNullOrEmpty(p.AccessCodeProtected)
                    ? ""
                    : SettingsStore.UnprotectSecret(p.AccessCodeProtected))).ToList();
            coreFleet = new Fleet(_realClients);
            foreach (var c in _realClients)
                _ = c.StartAsync();
        }
        else
        {
            coreFleet = DemoData.BuildFleet();
        }

        _host = new DemoHost(coreFleet);
        _host.Start();
        _tray = new TrayService(_host.Fleet, settings);
        _toasts = new ToastService(coreFleet, settings, _tray.ShowWindow);
        // first launch shows the full window; afterwards it lives in the tray
        _tray.ShowWindow();
    }
}
