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
    private SettingsStore? _settings;

    public App()
    {
        UnhandledException += (_, e) =>
            StartupLog.Step($"FATAL unhandled: {e.Exception.GetType().FullName}: {e.Exception.Message} :: {e.Exception.StackTrace}");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                StartupLog.Step($"FATAL appdomain: {ex.GetType().FullName}: {ex.Message} :: {ex.StackTrace}");
        };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            var ex = e.Exception.GetBaseException();
            StartupLog.Step($"FATAL unobserved task: {ex.GetType().FullName}: {ex.Message} :: {ex.StackTrace}");
        };
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
        _settings = settings;
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

        // CI diagnostics: parse each MainWindow section separately (opt-in via env)
        if (Environment.GetEnvironmentVariable("PLATEWATCH_XAML_PROBE") == "1")
            Probes.XamlProbe.Run(_host.Fleet);

        try
        {
            _tray = new TrayService(_host.Fleet, settings, ApplyFleetFromSettings, CleanupBeforeQuit);
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
            var window = new MainWindow(_host.Fleet, settings);
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

    /// <summary>Rebuilds the fleet from saved settings (real printers or demo)
    /// and reconnects, so Settings changes apply without a restart.</summary>
    private void ApplyFleetFromSettings()
    {
        if (_settings is null || _host is null) return;
        try
        {
            if (_realClients is not null)
            {
                foreach (var c in _realClients) c.Dispose();
                _realClients = null;
            }

            Fleet coreFleet;
            if (_settings.Settings.Printers.Count > 0)
            {
                _realClients = _settings.Settings.Printers.Select(p => new LanBambuClient(
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

            _host.Fleet.ReplaceFleet(coreFleet);
            _toasts?.ReplaceFleet(coreFleet);
            StartupLog.Step("fleet rebuilt from settings");
        }
        catch (Exception ex)
        {
            StartupLog.Write(ex);
        }
    }

    /// <summary>Stops background work (MQTT clients, toast registration) on real quit.</summary>
    private void CleanupBeforeQuit()
    {
        try
        {
            if (_realClients is not null)
                foreach (var c in _realClients) c.Dispose();
            _toasts?.Dispose();
        }
        catch
        {
            // quitting: never block exit on cleanup
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
