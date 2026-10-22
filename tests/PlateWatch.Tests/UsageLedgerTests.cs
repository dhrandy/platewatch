using PlateWatch.Core;

namespace PlateWatch.Tests;

public class UsageLedgerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "bambubar-tests-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    private static readonly SpoolKey Key = new("DEMO-P1S-001", 1, 1);

    [Fact]
    public void Records_accumulate_per_spool()
    {
        var ledger = new UsageLedger();
        ledger.RecordJob(Key, "PLA", "#E69F00", gramsUsed: 42, gramsRemaining: 300);
        ledger.RecordJob(Key, "PLA", "#E69F00", gramsUsed: 38, gramsRemaining: 262);
        var u = ledger.Lookup(Key)!;
        Assert.Equal(80, u.GramsUsed);
        Assert.Equal(2, u.JobsCompleted);
        Assert.Equal(262, u.EstimatedGramsRemaining);
    }

    [Fact]
    public void Estimate_is_null_before_any_job_and_sensible_after()
    {
        var ledger = new UsageLedger();
        Assert.Null(ledger.EstimateRemainingPrints(Key));
        ledger.RecordJob(Key, "PLA", "#E69F00", gramsUsed: 40, gramsRemaining: 320);
        var estimate = ledger.EstimateRemainingPrints(Key);
        Assert.NotNull(estimate);
        Assert.Equal(8.0, estimate!.Value, 3);   // "about 8 more of these"
    }

    [Fact]
    public void Json_roundtrip_survives_save_and_load()
    {
        var path = Path.Combine(_dir, "usage.json");
        var ledger = new UsageLedger();
        ledger.RecordJob(Key, "PLA", "#E69F00", 42, 300);
        ledger.RecordJob(new SpoolKey("DEMO-X1C-003", 1, 2), "PETG", "#CC79A7", 55, 100);
        ledger.Save(path);

        var loaded = UsageLedger.Load(path);
        Assert.Equal(2, loaded.TrackedSpoolCount);
        Assert.Equal(42, loaded.Lookup(Key)!.GramsUsed);
        Assert.Equal(55, loaded.Lookup(new SpoolKey("DEMO-X1C-003", 1, 2))!.GramsUsed);
    }

    [Fact]
    public void Missing_file_loads_empty_ledger()
    {
        var loaded = UsageLedger.Load(Path.Combine(_dir, "nope.json"));
        Assert.Equal(0, loaded.TrackedSpoolCount);
    }
}
