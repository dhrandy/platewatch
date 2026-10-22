using System.Text.Json;

namespace PlateWatch.Core;

public sealed record SpoolKey(string PrinterSerial, int AmsUnit, int Slot)
{
    public override string ToString() => $"{PrinterSerial}|{AmsUnit}|{Slot}";
    public static SpoolKey Parse(string s)
    {
        var parts = s.Split('|');
        return new SpoolKey(parts[0], int.Parse(parts[1]), int.Parse(parts[2]));
    }
}

public sealed class SpoolUsage
{
    public string Material { get; set; } = "";
    public string ColorHex { get; set; } = "";
    public double GramsUsed { get; set; }
    public int JobsCompleted { get; set; }
    public double EstimatedGramsRemaining { get; set; }
}

/// <summary>
/// Local JSON record of what each spool has printed, so "how much is left"
/// estimates keep improving without any cloud account.
/// </summary>
public sealed class UsageLedger
{
    private readonly Dictionary<string, SpoolUsage> _bySpool = new();

    public void RecordJob(SpoolKey key, string material, string colorHex,
                          double gramsUsed, double gramsRemaining)
    {
        if (!_bySpool.TryGetValue(key.ToString(), out var u))
        {
            u = new SpoolUsage { Material = material, ColorHex = colorHex };
            _bySpool[key.ToString()] = u;
        }
        u.GramsUsed += gramsUsed;
        u.JobsCompleted += 1;
        u.EstimatedGramsRemaining = gramsRemaining;
    }

    public SpoolUsage? Lookup(SpoolKey key)
        => _bySpool.TryGetValue(key.ToString(), out var u) ? u : null;

    /// <summary>"About 3 more of these" - null until the spool has finished a job.</summary>
    public double? EstimateRemainingPrints(SpoolKey key)
    {
        var u = Lookup(key);
        if (u is null || u.JobsCompleted == 0 || u.GramsUsed <= 0) return null;
        var avgGramsPerJob = u.GramsUsed / u.JobsCompleted;
        return u.EstimatedGramsRemaining / avgGramsPerJob;
    }

    public int TrackedSpoolCount => _bySpool.Count;

    public static UsageLedger Load(string path)
    {
        var ledger = new UsageLedger();
        if (!File.Exists(path)) return ledger;
        var data = JsonSerializer.Deserialize<Dictionary<string, SpoolUsage>>(File.ReadAllText(path));
        if (data is not null)
            foreach (var kv in data) ledger._bySpool[kv.Key] = kv.Value;
        return ledger;
    }

    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(_bySpool,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
