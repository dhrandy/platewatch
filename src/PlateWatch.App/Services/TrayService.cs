using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PlateWatch.App.ViewModels;
using PlateWatch.Core;

namespace PlateWatch.App.Services;

/// <summary>
/// Owns the tray icon: dynamic status ring icon, tooltip, fleet context menu,
/// left-click opens the popup for the selected printer.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly FleetViewModel _fleet;
    private readonly TaskbarIcon _icon;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly SettingsStore _settings;
    private readonly Action? _applyFleet;
    private readonly Action? _onQuit;
    private MainWindow? _window;
    private PopupWindow? _popup;
    private SettingsWindow? _settingsWindow;
    private bool _quitting;

    public TrayService(FleetViewModel fleet, SettingsStore settings,
        Action? applyFleet = null, Action? onQuit = null)
    {
        _fleet = fleet;
        _settings = settings;
        _applyFleet = applyFleet;
        _onQuit = onQuit;
        _icon = new TaskbarIcon
        {
            ToolTipText = "PlateWatch",
            ContextFlyout = BuildMenu(),
            LeftClickCommand = new RelayCommand(ShowPopup),
        };
        _icon.ForceCreate();
        UpdateIcon();
        _timer.Tick += (_, _) => UpdateIcon();
        _timer.Start();
    }

    public void ShowWindow()
    {
        if (_window is null)
        {
            _window = new MainWindow(_fleet, _settings, ShowSettings);
            StartupLog.Step("window created");
            _window.AppWindow.Closing += (_, args) =>
            {
                // tray app: closing the window hides it; Quit exits.
                // during a real quit the close must go through, or
                // Application.Exit() gets cancelled here and the process
                // stays alive in the background.
                if (_quitting) return;
                args.Cancel = true;
                _window.AppWindow.Hide();
            };
            _window.Closed += (_, _) => _window = null;
        }
        _window.AppWindow.Show();
        _window.Activate();
    }

    public void ShowPopup()
    {
        if (_fleet.Selected is null) return;
        _popup = new PopupWindow(_fleet.Selected);
        _popup.ShowAtTray();
    }

    public void ShowSettings()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_settings, _applyFleet);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }
        _settingsWindow.AppWindow.Show();
        _settingsWindow.Activate();
    }

    private MenuFlyout BuildMenu()
    {
        var menu = new MenuFlyout();
        // Flyouts share the window's XAML tree in WinAppSDK 2.x, so the
        // window content's RequestedTheme already cascades to this menu.
        menu.Opening += (_, _) => RebuildFleetItems(menu);
        menu.Items.Add(new MenuFlyoutItem { Text = "Open PlateWatch", Command = new RelayCommand(ShowWindow) });
        menu.Items.Add(new MenuFlyoutItem { Text = "Settings…", Command = new RelayCommand(ShowSettings) });
        menu.Items.Add(new MenuFlyoutSeparator());
        var quit = new MenuFlyoutItem { Text = "Quit" };
        quit.Click += (_, _) => Quit();
        menu.Items.Add(quit);
        return menu;
    }

    private void RebuildFleetItems(MenuFlyout menu)
    {
        // keep Open + separator + Quit at the end, refresh printer items above them
        for (var i = menu.Items.Count - 4; i >= 0; i--)
            menu.Items.RemoveAt(i);
        var index = 0;
        foreach (var p in _fleet.Printers)
        {
            var item = new ToggleMenuFlyoutItem
            {
                Text = $"{p.Name} — {p.ChipLine}",
                IsChecked = ReferenceEquals(p, _fleet.Selected),
                Command = p.SelectCommand,
            };
            menu.Items.Insert(index++, item);
        }
        if (index > 0)
            menu.Items.Insert(index, new MenuFlyoutSeparator());
    }

    private void UpdateIcon()
    {
        var p = _fleet.Selected;
        if (p is null) return;
        var s = p.Client.State;
        var style = DesignTokens.StyleFor(s.Status);
        // H.NotifyIcon's IconSource path throws NotImplementedException for
        // WriteableBitmap on WinUI 3 (crash 0xC000027B). Assign the native
        // System.Drawing.Icon instead, built from the same pixels.
        var pixels = TrayIconRenderer.RenderPixels(s.Status, s.Progress, style.ColorHex);
        var oldIcon = _icon.Icon;
        _icon.Icon = new System.Drawing.Icon(new MemoryStream(IconFile.EncodeIco(pixels, TrayIconRenderer.Size)));
        oldIcon?.Dispose();
        _icon.ToolTipText = $"PlateWatch — {p.Name}: {style.Label}" +
            (s.Status is PlateWatch.Core.PrinterStatus.Printing or PlateWatch.Core.PrinterStatus.Paused
                ? $" {p.PercentText}%"
                : "");
    }

    private void Quit()
    {
        _quitting = true;
        _timer.Stop();
        _icon.Dispose();
        _onQuit?.Invoke();
        Application.Current.Exit();
    }

    public void Dispose()
    {
        _timer.Stop();
        _icon.Dispose();
    }
}
