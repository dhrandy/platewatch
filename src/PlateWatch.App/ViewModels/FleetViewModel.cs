using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PlateWatch.Core;

namespace PlateWatch.App.ViewModels;

public partial class FleetViewModel : ObservableObject
{
    private Fleet _fleet;

    public FleetViewModel(Fleet fleet)
    {
        _fleet = fleet;
        Printers = new ObservableCollection<PrinterViewModel>();
        RebuildPrinters();
    }

    /// <summary>Swaps the underlying fleet in place (settings saved) without
    /// recreating this view model, so windows and the tray keep their bindings.</summary>
    public void ReplaceFleet(Fleet fleet)
    {
        _fleet = fleet;
        RebuildPrinters();
    }

    private void RebuildPrinters()
    {
        Printers.Clear();
        foreach (var p in _fleet.Printers.Select(c => new PrinterViewModel(c)))
        {
            p.RequestSelect = () => Selected = p;
            Printers.Add(p);
        }
        Selected = Printers.FirstOrDefault();
    }

    public ObservableCollection<PrinterViewModel> Printers { get; }

    public Fleet Core => _fleet;

    [ObservableProperty] private PrinterViewModel? _selected;
    [ObservableProperty] private string _updatedText = "Updated just now";

    partial void OnSelectedChanged(PrinterViewModel? value)
    {
        foreach (var p in Printers)
            p.IsSelected = ReferenceEquals(p, value);
    }

    public string BusiestName => _fleet.Busiest?.Identity.Name ?? "";

    public void TickAll(DateTimeOffset now)
    {
        foreach (var p in _fleet.Printers)
            if (p is MockBambuClient m) m.Tick(now);

        if (_fleet.Printers.Count > 0)
        {
            var ago = now - _fleet.Printers.Max(p => p.State.LastUpdated);
            UpdatedText = ago.TotalSeconds < 5 ? "Updated just now"
                : ago.TotalMinutes < 1 ? $"Updated {(int)ago.TotalSeconds}s ago"
                : $"Updated {(int)ago.TotalMinutes}m ago";
        }
    }
}
