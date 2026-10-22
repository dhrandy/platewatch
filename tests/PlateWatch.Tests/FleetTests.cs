using PlateWatch.Core;

namespace PlateWatch.Tests;

public class FleetTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Demo_fleet_matches_the_approved_render()
    {
        var fleet = DemoData.BuildFleet(T0);
        Assert.Equal(3, fleet.Printers.Count);
        var p1s = fleet.Printers[0];
        Assert.Equal("P1S (demo)", p1s.Identity.Name);
        Assert.Equal(PrinterStatus.Printing, p1s.State.Status);
        Assert.Equal(0.63, p1s.State.Progress, 2);
        Assert.Equal(3540, p1s.State.SecondsLeft, 0);  // 59m left at 63%
        Assert.Equal(179, p1s.State.LayerCurrent);
        Assert.Equal(284, p1s.State.LayerTotal);
        Assert.Equal(PrinterStatus.Idle, fleet.Printers[1].State.Status);
        Assert.Equal(PrinterStatus.Finished, fleet.Printers[2].State.Status);
    }

    [Fact]
    public void Busiest_is_the_printing_printer()
    {
        var fleet = DemoData.BuildFleet(T0);
        Assert.Equal("P1S (demo)", fleet.Busiest?.Identity.Name);
    }

    [Fact]
    public void Busiest_falls_back_to_first_when_nothing_prints()
    {
        var idle = new MockBambuClient(new MockPrinterSpec
        {
            Identity = new PrinterIdentity("A", "M", "S1", "10.0.0.1", "1"),
            Status = PrinterStatus.Idle,
        }, T0);
        var fleet = new Fleet(new IBambuClient[] { idle });
        Assert.Same(idle, fleet.Busiest);
    }

    [Fact]
    public void Demo_ams_matches_the_render_spools()
    {
        var fleet = DemoData.BuildFleet(T0);
        var ams = fleet.Printers[0].State.AmsUnits.Single();
        Assert.Equal("AMS 2 PRO A", ams.Name);
        Assert.Equal(16, ams.HumidityPct);
        Assert.Equal(4, ams.Spools.Count);
        Assert.True(ams.Spools[0].Active);
        Assert.True(ams.Spools[3].IsLow);      // 12% spool is flagged low
        Assert.False(ams.Spools[0].IsLow);
    }

    [Fact]
    public void Fleet_aggregates_state_changed()
    {
        var fleet = DemoData.BuildFleet(T0);
        IBambuClient? changed = null;
        fleet.StateChanged += (_, client) => changed = client;
        ((MockBambuClient)fleet.Printers[0]).Tick(T0.AddSeconds(10));
        Assert.Same(fleet.Printers[0], changed);
    }
}
