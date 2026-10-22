using PlateWatch.Core;

namespace PlateWatch.Tests;

public class NotificationPolicyTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    private static PrinterState With(PrinterStatus s) => new() { Status = s };

    [Fact]
    public void Finish_emits_once_per_episode()
    {
        var policy = new NotificationPolicy();
        Assert.True(policy.ShouldNotify("p1", With(PrinterStatus.Printing), With(PrinterStatus.Finished), T0, out var kind));
        Assert.Equal(PrinterEventKind.PrintFinished, kind);
        // same transition inside the cooldown does not fire again
        Assert.False(policy.ShouldNotify("p1", With(PrinterStatus.Printing), With(PrinterStatus.Finished), T0.AddMinutes(3), out _));
    }

    [Fact]
    public void New_episode_after_cooldown_fires_again()
    {
        var policy = new NotificationPolicy { Cooldown = TimeSpan.FromMinutes(15) };
        policy.ShouldNotify("p1", With(PrinterStatus.Printing), With(PrinterStatus.Finished), T0, out _);
        Assert.True(policy.ShouldNotify("p1", With(PrinterStatus.Printing), With(PrinterStatus.Finished), T0.AddMinutes(16), out _));
    }

    [Fact]
    public void Error_and_pause_emit()
    {
        var policy = new NotificationPolicy();
        Assert.True(policy.ShouldNotify("p1", With(PrinterStatus.Printing), With(PrinterStatus.Error), T0, out var k1));
        Assert.Equal(PrinterEventKind.PrintError, k1);
        Assert.True(policy.ShouldNotify("p1", With(PrinterStatus.Printing), With(PrinterStatus.Paused), T0, out var k2));
        Assert.Equal(PrinterEventKind.PrintPaused, k2);
    }

    [Fact]
    public void No_status_change_means_no_toast()
    {
        var policy = new NotificationPolicy();
        Assert.False(policy.ShouldNotify("p1", With(PrinterStatus.Printing), With(PrinterStatus.Printing), T0, out _));
    }

    [Fact]
    public void Print_started_is_opt_in()
    {
        var off = new NotificationPolicy();
        Assert.False(off.ShouldNotify("p1", With(PrinterStatus.Idle), With(PrinterStatus.Printing), T0, out _));
        var on = new NotificationPolicy { EnablePrintStarted = true };
        Assert.True(on.ShouldNotify("p1", With(PrinterStatus.Idle), With(PrinterStatus.Printing), T0, out var kind));
        Assert.Equal(PrinterEventKind.PrintStarted, kind);
    }

    [Fact]
    public void Cooldowns_are_per_printer()
    {
        var policy = new NotificationPolicy();
        Assert.True(policy.ShouldNotify("p1", With(PrinterStatus.Printing), With(PrinterStatus.Finished), T0, out _));
        Assert.True(policy.ShouldNotify("p2", With(PrinterStatus.Printing), With(PrinterStatus.Finished), T0, out _));
    }
}
