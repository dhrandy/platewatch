using System.Security;
using Microsoft.Windows.AppNotifications;
using PlateWatch.Core;

namespace PlateWatch.App.Services;

/// <summary>
/// Watches the fleet for state transitions (via the core NotificationPolicy:
/// one toast per event, 15 min cooldown) and raises Windows toasts. Print
/// start toasts are opt-in from settings.
/// </summary>
public sealed class ToastService : IDisposable
{
    private readonly NotificationPolicy _policy = new() { EnablePrintStarted = true };
    private readonly Dictionary<string, PrinterState> _prev = new();
    private readonly List<IBambuClient> _subscribed = new();
    private readonly SettingsStore _settings;

    public ToastService(Fleet fleet, SettingsStore settings, Action onInvoked)
    {
        _settings = settings;
        // register before subscribing: if registration fails (a missing
        // WinAppSDK resource dll on first run), no half-built service stays
        // subscribed to fleet events
        AppNotificationManager.Default.Register();
        AppNotificationManager.Default.NotificationInvoked += (_, _) => onInvoked();
        Subscribe(fleet);
    }

    private void Subscribe(Fleet fleet)
    {
        foreach (var client in fleet.Printers)
        {
            _prev[client.Identity.Serial] = client.State.Clone();
            client.StateChanged += OnStateChanged;
            _subscribed.Add(client);
        }
    }

    /// <summary>Moves the watch to a new fleet (settings saved).</summary>
    public void ReplaceFleet(Fleet fleet)
    {
        foreach (var client in _subscribed)
            client.StateChanged -= OnStateChanged;
        _subscribed.Clear();
        _prev.Clear();
        Subscribe(fleet);
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (sender is not IBambuClient client) return;
        var prev = _prev[client.Identity.Serial];
        var next = client.State;
        if (_settings.Settings.ToastsEnabled &&
            _policy.ShouldNotify(client.Identity.Serial, prev, next, DateTimeOffset.Now, out var kind))
        {
            if (kind != PrinterEventKind.PrintStarted || _settings.Settings.ToastOnPrintStart)
                Show(client, next, kind);
        }
        _prev[client.Identity.Serial] = next.Clone();
    }

    private static void Show(IBambuClient client, PrinterState state, PrinterEventKind kind)
    {
        var (title, body) = kind switch
        {
            PrinterEventKind.PrintFinished =>
                ($"{client.Identity.Name}: print finished", state.JobName),
            PrinterEventKind.PrintError =>
                ($"{client.Identity.Name}: needs attention",
                 string.IsNullOrEmpty(state.ErrorText) ? state.JobName : state.ErrorText),
            PrinterEventKind.PrintPaused =>
                ($"{client.Identity.Name}: print paused", state.JobName),
            _ =>
                ($"{client.Identity.Name}: print started", state.JobName),
        };
        var xml = "<toast><visual><binding template=\"ToastGeneric\">" +
                  $"<text>{SecurityElement.Escape(title)}</text>" +
                  $"<text>{SecurityElement.Escape(body)}</text>" +
                  "</binding></visual></toast>";
        AppNotificationManager.Default.Show(new AppNotification(xml));
    }

    public void Dispose() => AppNotificationManager.Default.Unregister();
}
