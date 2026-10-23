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
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            StartupLog.Write(ex);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
        var settings = SettingsStore.Load();
        // generate the .ico (windows + taskbar) before any window is created
        IconFile.EnsureAsync().GetAwaiter().GetResult();
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
        catch (Exception ex)
        {
            StartupLog.Write(ex);
            throw;
        }
    }
}

/// <summary>
/// Last-resort diagnostics: a WinExe that crashes before any window appears
/// shows nothing at all. Write the exception to
/// %APPDATA%/PlateWatch/startup-error.log so a silent startup failure is
/// readable after the fact.
/// </summary>
internal static class StartupLog
{
    public static void Write(Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlateWatch");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "startup-error.log"),
                DateTimeOffset.Now + Environment.NewLine + ex);
        }
        catch
        {
            // never throw from the logger itself
        }
    }
}
