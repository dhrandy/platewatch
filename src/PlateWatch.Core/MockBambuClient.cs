namespace PlateWatch.Core;

public sealed class MockPrinterSpec
{
    public required PrinterIdentity Identity { get; init; }
    public PrinterStatus Status { get; init; } = PrinterStatus.Printing;
    public string JobName { get; init; } = "";
    public string Material { get; init; } = "";
    public string Plate { get; init; } = "";
    public double InitialProgress { get; init; }
    public int LayerTotal { get; init; }
    public double TotalSeconds { get; init; } = 3600;
    public double TimeScale { get; init; } = 8;   // demo speed: 1 real second = 8 print seconds
    public double NozzleC { get; init; } = 220;
    public double BedC { get; init; } = 55;
    public double? ChamberC { get; init; }  // null = no chamber sensor (A1/A1 mini)
    public double FanPartPct { get; init; } = 90;
    public double FanAuxPct { get; init; } = 40;
    public string FilamentColorHex { get; init; } = "#E69F00";
    public List<AmsUnit> AmsUnits { get; init; } = new();

    public PrinterState InitialState()
    {
        var secondsLeft = Status == PrinterStatus.Printing ? TotalSeconds * (1 - InitialProgress) : 0;
        return new PrinterState
        {
            Status = Status,
            JobName = JobName,
            Material = Material,
            Plate = Plate,
            Progress = Status == PrinterStatus.Finished ? 1.0 : InitialProgress,
            LayerTotal = LayerTotal,
            LayerCurrent = (int)Math.Round(InitialProgress * LayerTotal),
            SecondsLeft = secondsLeft,
            NozzleC = Status == PrinterStatus.Idle ? 28 : NozzleC,
            NozzleTargetC = Status == PrinterStatus.Idle ? 0 : NozzleC,
            BedC = Status == PrinterStatus.Idle ? 26 : BedC,
            BedTargetC = Status == PrinterStatus.Idle ? 0 : BedC,
            ChamberC = ChamberC,
            FanPartPct = Status == PrinterStatus.Idle ? 0 : FanPartPct,
            FanAuxPct = Status == PrinterStatus.Idle ? 0 : FanAuxPct,
            FilamentColorHex = FilamentColorHex,
            AmsUnits = AmsUnits,
        };
    }
}

/// <summary>A pretend printer that ticks a simulated job in real time.</summary>
public sealed class MockBambuClient : IBambuClient
{
    private readonly MockPrinterSpec _spec;
    private DateTimeOffset _lastTick;

    public MockBambuClient(MockPrinterSpec spec, DateTimeOffset? start = null)
    {
        _spec = spec;
        State = spec.InitialState();
        _lastTick = start ?? DateTimeOffset.UtcNow;
    }

    public PrinterIdentity Identity => _spec.Identity;
    public PrinterState State { get; private set; }
    public event EventHandler? StateChanged;

    public void Tick(DateTimeOffset now)
    {
        var dt = now - _lastTick;
        _lastTick = now;
        if (State.Status != PrinterStatus.Printing || dt <= TimeSpan.Zero) return;

        State.SecondsLeft = Math.Max(0, State.SecondsLeft - dt.TotalSeconds * _spec.TimeScale);
        State.Progress = Math.Min(1.0, 1.0 - State.SecondsLeft / _spec.TotalSeconds);
        State.LayerCurrent = Math.Min(State.LayerTotal, (int)Math.Round(State.Progress * State.LayerTotal));
        State.LastUpdated = now;
        if (State.SecondsLeft <= 0)
        {
            State.Status = PrinterStatus.Finished;
            State.Progress = 1.0;
            State.LayerCurrent = State.LayerTotal;
        }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task PauseAsync()
    {
        if (State.Status == PrinterStatus.Printing) State.Status = PrinterStatus.Paused;
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (State.Status == PrinterStatus.Paused)
        {
            State.Status = PrinterStatus.Printing;
            _lastTick = DateTimeOffset.UtcNow;
        }
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        State.Status = PrinterStatus.Idle;
        State.JobName = "";
        State.Progress = 0;
        State.LayerCurrent = 0;
        State.SecondsLeft = 0;
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task SetSpeedAsync(PrintSpeed speed)
    {
        State.Speed = speed;
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task SetLightAsync(bool on)
    {
        State.LightOn = on;
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
