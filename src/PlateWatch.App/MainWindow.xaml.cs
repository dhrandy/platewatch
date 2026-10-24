using Microsoft.UI.Xaml;
using PlateWatch.App.Services;
using Microsoft.UI.Xaml.Media;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App;

/// <summary>
/// The full window: fleet chips across the top, hero card for the selected
/// printer, temps and filament cards on the right. Matches the approved
/// c1v2 window render.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly SettingsStore _settings;
    private readonly Action? _openSettings;
    private readonly DispatcherTimer _saveDebounce = new() { Interval = TimeSpan.FromMilliseconds(800) };

    public MainWindow(FleetViewModel fleet, SettingsStore settings, Action? openSettings = null)
    {
        Fleet = fleet;
        _settings = settings;
        _openSettings = openSettings;
        InitializeComponent();
        Title = "PlateWatch";
        SystemBackdrop = new MicaBackdrop();

        var appWindow = AppWindow;
        if (IconFile.CurrentPath is not null)
            appWindow.SetIcon(IconFile.CurrentPath);
        appWindow.Resize(new Windows.Graphics.SizeInt32(
            Math.Clamp(settings.Settings.WindowWidth, 860, 3840),
            Math.Clamp(settings.Settings.WindowHeight, 620, 2160)));
        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 860;
            presenter.PreferredMinimumHeight = 620;
        }

        ApplyTheme();
        ThemeService.Changed += (_, _) => ApplyTheme();

        _saveDebounce.Tick += (_, _) => { _saveDebounce.Stop(); _settings.Save(); };
        appWindow.Changed += (_, args) =>
        {
            if (!args.DidSizeChange) return;
            _settings.Settings.WindowWidth = appWindow.Size.Width;
            _settings.Settings.WindowHeight = appWindow.Size.Height;
            _saveDebounce.Stop();
            _saveDebounce.Start();
        };

        Closed += (_, _) =>
        {
            _settings.Save();
            Application.Current.Exit();
        };
    }

    public FleetViewModel Fleet { get; }

    private void ApplyTheme() => ThemeService.ApplyTo(this);

    private void OnSettingsClick(object sender, RoutedEventArgs e) => _openSettings?.Invoke();
}
