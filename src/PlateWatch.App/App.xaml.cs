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
        StartupLog.Step("launched");
        try
        {
        var settings = SettingsStore.Load();
        StartupLog.Step("settings loaded");
        // generate the .ico (windows + taskbar) before any window is created;
        // icon failure must never take down startup
        try
        {
            IconFile.Ensure();
            StartupLog.Step("icon ready");
        }
        catch (Exception ex)
        {
            StartupLog.Write(ex);
        }
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

        StartupLog.Step("fleet ready");
        _host = new DemoHost(coreFleet);
        _host.Start();

        try
        {
            _tray = new TrayService(_host.Fleet, settings);
            StartupLog.Step("tray ready");
        }
        catch (Exception ex)
        {
            StartupLog.Write(ex);
        }

        try
        {
            _toasts = new ToastService(coreFleet, settings, () => _tray?.ShowWindow());
            StartupLog.Step("toasts ready");
        }
        catch (Exception ex)
        {
            StartupLog.Write(ex);
        }

        // first launch shows the full window; afterwards it lives in the tray.
        // show the window even when the tray icon failed, or the app would
        // be running with no UI at all.
        if (_tray is not null)
        {
            _tray.ShowWindow();
        }
        else
        {
            var window = new MainWindow(_host.Fleet);
            window.AppWindow.Show();
            window.Activate();
        }
        StartupLog.Step("window shown");
        }
        catch (Exception ex)
        {
            StartupLog.Write(ex);
            throw;
        }
    }
}

/// <summary>
/// Last-resort diagnostics: a WinExe that fails before any window appears
/// shows nothing at all. Append startup steps and any exception to
/// %APPDATA%/PlateWatch/startup.log so a silent startup failure (or a hang
/// between steps) is readable after the fact.
/// </summary>
internal static class StartupLog
{
    public static void Write(Exception ex) => Append("ERROR " + ex);

    public static void Step(string step) => Append(step);

    private static void Append(string line)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlateWatch");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "startup.log"),
                DateTimeOffset.Now + " " + line + Environment.NewLine);
        }
        catch
        {
            // never throw from the logger itself
        }
    }
}
