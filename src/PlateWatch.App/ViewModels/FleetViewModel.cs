using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PlateWatch.Core;

namespace PlateWatch.App.ViewModels;

public partial class FleetViewModel : ObservableObject
{
    private readonly Fleet _fleet;

    public FleetViewModel(Fleet fleet)
    {
        _fleet = fleet;
        Printers = new ObservableCollection<PrinterViewModel>(
            fleet.Printers.Select(p => new PrinterViewModel(p)));
        Selected = Printers.FirstOrDefault();
    }

    public ObservableCollection<PrinterViewModel> Printers { get; }

    [ObservableProperty] private PrinterViewModel? _selected;

    public string BusiestName => _fleet.Busiest?.Identity.Name ?? "";

    public void TickAll(DateTimeOffset now)
    {
        foreach (var p in _fleet.Printers)
            if (p is MockBambuClient m) m.Tick(now);
    }
}
