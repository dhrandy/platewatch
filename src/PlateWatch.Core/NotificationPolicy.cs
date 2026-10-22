namespace PlateWatch.Core;

public enum PrinterEventKind { PrintFinished, PrintError, PrintPaused, PrintStarted }

/// <summary>
/// Turns state transitions into at most one toast per event per cooldown, so a
/// printer flapping between states cannot spam notifications.
/// </summary>
public sealed class NotificationPolicy
{
    private readonly Dictionary<(string Printer, PrinterEventKind Kind), DateTimeOffset> _lastSent = new();

    public TimeSpan Cooldown { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>"Print started" toasts are opt-in (matches the approved plan).</summary>
    public bool EnablePrintStarted { get; init; }

    public PrinterEventKind? Classify(PrinterState prev, PrinterState next)
    {
        if (prev.Status == next.Status) return null;
        return next.Status switch
        {
            PrinterStatus.Finished => PrinterEventKind.PrintFinished,
            PrinterStatus.Error => PrinterEventKind.PrintError,
            PrinterStatus.Paused => PrinterEventKind.PrintPaused,
            PrinterStatus.Printing when prev.Status is PrinterStatus.Idle or PrinterStatus.Finished
                => PrinterEventKind.PrintStarted,
            _ => null,
        };
    }

    public bool ShouldNotify(string printerId, PrinterState prev, PrinterState next,
                             DateTimeOffset now, out PrinterEventKind kind)
    {
        kind = default;
        var classified = Classify(prev, next);
        if (classified is null) return false;
        if (classified == PrinterEventKind.PrintStarted && !EnablePrintStarted) return false;

        kind = classified.Value;
        var key = (printerId, kind);
        if (_lastSent.TryGetValue(key, out var last) && now - last < Cooldown) return false;
        _lastSent[key] = now;
        return true;
    }
}
