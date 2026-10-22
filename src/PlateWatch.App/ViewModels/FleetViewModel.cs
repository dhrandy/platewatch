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
        foreach (var p in Printers)
            p.RequestSelect = () => Selected = p;
        Selected = Printers.FirstOrDefault();
    }

    public ObservableCollection<PrinterViewModel> Printers { get; }

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
    }
}
