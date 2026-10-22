using Microsoft.UI.Xaml;
using PlateWatch.App.ViewModels;
using PlateWatch.Core;

namespace PlateWatch.App.Services;

/// <summary>Runs the demo fleet on a 1s timer until the real LAN client lands (M5).</summary>
public sealed class DemoHost
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public DemoHost(Fleet fleet)
    {
        Fleet = new FleetViewModel(fleet);
        _timer.Tick += (_, _) => Fleet.TickAll(DateTimeOffset.Now);
    }

    public FleetViewModel Fleet { get; }

    public void Start() => _timer.Start();
}
