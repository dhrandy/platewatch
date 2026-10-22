namespace PlateWatch.Core;

/// <summary>All configured printers. The tray icon shows the busiest one.</summary>
public sealed class Fleet
{
    public Fleet(IReadOnlyList<IBambuClient> printers)
    {
        Printers = printers;
        foreach (var p in printers)
            p.StateChanged += (_, _) => StateChanged?.Invoke(this, p);
    }

    public IReadOnlyList<IBambuClient> Printers { get; }

    /// <summary>Fires with the client whose state changed.</summary>
    public event EventHandler<IBambuClient>? StateChanged;

    /// <summary>Printing job with the most progress, else the first printer.</summary>
    public IBambuClient? Busiest =>
        Printers.Where(p => p.State.Status == PrinterStatus.Printing)
                .OrderByDescending(p => p.State.Progress)
                .FirstOrDefault()
        ?? Printers.FirstOrDefault();
}
