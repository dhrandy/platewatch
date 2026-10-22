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
    private MainWindow? _window;
    private PopupWindow? _popup;

    public TrayService(FleetViewModel fleet)
    {
        _fleet = fleet;
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
            _window = new MainWindow(_fleet);
            _window.AppWindow.Closing += (_, args) =>
            {
                // tray app: closing the window hides it; Quit exits
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

    private MenuFlyout BuildMenu()
    {
        var menu = new MenuFlyout();
        menu.Opening += (_, _) => RebuildFleetItems(menu);
        menu.Items.Add(new MenuFlyoutItem { Text = "Open PlateWatch", Command = new RelayCommand(ShowWindow) });
        menu.Items.Add(new MenuFlyoutSeparator());
        var quit = new MenuFlyoutItem { Text = "Quit" };
        quit.Click += (_, _) => Application.Current.Exit();
        menu.Items.Add(quit);
        return menu;
    }

    private void RebuildFleetItems(MenuFlyout menu)
    {
        // keep Open + separator + Quit at the end, refresh printer items above them
        for (var i = menu.Items.Count - 3; i >= 0; i--)
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
        _icon.IconSource = TrayIconRenderer.Render(s.Status, s.Progress, style.ColorHex);
        _icon.ToolTipText = $"PlateWatch — {p.Name}: {style.Label}" +
            (s.Status is PlateWatch.Core.PrinterStatus.Printing or PlateWatch.Core.PrinterStatus.Paused
                ? $" {p.PercentText}%"
                : "");
    }

    public void Dispose()
    {
        _timer.Stop();
        _icon.Dispose();
    }
}
