using PlateWatch.Core;

namespace PlateWatch.Tests;

public class MockPrinterTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    private static MockBambuClient Printing(double timeScale = 8) => new(new MockPrinterSpec
    {
        Identity = new PrinterIdentity("P1S (demo)", "Bambu Lab P1S", "DEMO-P1S-001",
                                       "192.168.1.51", "01.03.00.00"),
        Status = PrinterStatus.Printing,
        JobName = "Articulated Dragon",
        InitialProgress = 0.63,
        LayerTotal = 284,
        TotalSeconds = 3540.0 / (1 - 0.63),
        TimeScale = timeScale,
    }, T0);

    [Fact]
    public void Tick_advances_progress_and_counts_down()
    {
        var p = Printing(timeScale: 1);
        p.Tick(T0.AddSeconds(60));
        Assert.True(p.State.Progress > 0.63);
        Assert.True(p.State.SecondsLeft < 3540);
        Assert.True(p.State.LayerCurrent >= 179);
    }

    [Fact]
    public void Tick_reaches_finished_at_zero_seconds_left()
    {
        var p = Printing(timeScale: 1);
        p.Tick(T0.AddSeconds(3541));
        Assert.Equal(PrinterStatus.Finished, p.State.Status);
        Assert.Equal(1.0, p.State.Progress);
        Assert.Equal(p.State.LayerTotal, p.State.LayerCurrent);
    }

    [Fact]
    public async Task Pause_freezes_the_job_and_resume_continues()
    {
        var p = Printing(timeScale: 1);
        await p.PauseAsync();
        var frozen = p.State.SecondsLeft;
        p.Tick(T0.AddSeconds(600));
        Assert.Equal(frozen, p.State.SecondsLeft);
        Assert.Equal(PrinterStatus.Paused, p.State.Status);
        await p.ResumeAsync();
        Assert.Equal(PrinterStatus.Printing, p.State.Status);
    }

    [Fact]
    public async Task Stop_clears_the_job_to_idle()
    {
        var p = Printing();
        await p.StopAsync();
        Assert.Equal(PrinterStatus.Idle, p.State.Status);
        Assert.Equal("", p.State.JobName);
        Assert.Equal(0, p.State.Progress);
        Assert.Equal(0, p.State.SecondsLeft);
    }

    [Fact]
    public async Task Speed_and_light_controls_apply()
    {
        var p = Printing();
        await p.SetSpeedAsync(PrintSpeed.Ludicrous);
        Assert.Equal(PrintSpeed.Ludicrous, p.State.Speed);
        await p.SetLightAsync(false);
        Assert.False(p.State.LightOn);
    }

    [Fact]
    public void StateChanged_fires_on_tick()
    {
        var p = Printing(timeScale: 1);
        var fired = 0;
        p.StateChanged += (_, _) => fired++;
        p.Tick(T0.AddSeconds(5));
        Assert.Equal(1, fired);
    }
}
