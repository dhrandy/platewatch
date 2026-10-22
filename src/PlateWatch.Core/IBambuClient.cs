namespace PlateWatch.Core;

/// <summary>
/// One printer connection. MockBambuClient simulates; a real build swaps in a
/// LAN client over the printer's local MQTT feed (ssl:8883, access code),
/// the same channel Bambu Studio uses in LAN mode. No cloud, no account.
/// </summary>
public interface IBambuClient
{
    PrinterIdentity Identity { get; }
    PrinterState State { get; }
    event EventHandler? StateChanged;

    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
    Task SetSpeedAsync(PrintSpeed speed);
    Task SetLightAsync(bool on);
}
