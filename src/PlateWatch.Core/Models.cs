namespace PlateWatch.Core;

/// <summary>Printer lifecycle states. Style is color + glyph + word, never color alone.</summary>
public enum PrinterStatus { Printing, Paused, Finished, Error, Idle }

public enum PrintSpeed { Silent, Standard, Sport, Ludicrous }

public sealed record PrinterIdentity(string Name, string Model, string Serial, string IpAddress, string Firmware);

public sealed class Spool
{
    public int Slot { get; set; }
    public string ColorHex { get; set; } = "#9AA0A6";
    public string Material { get; set; } = "PLA";
    public double Remaining01 { get; set; } = 1.0;
    public bool Active { get; set; }
    public bool IsLow => Remaining01 <= 0.15;
}

public sealed class AmsUnit
{
    public string Name { get; set; } = "AMS";
    public double? HumidityPct { get; set; }
    public double? TemperatureC { get; set; }
    public List<Spool> Spools { get; set; } = new();
}

public sealed class PrinterState
{
    public PrinterStatus Status { get; set; } = PrinterStatus.Idle;
    public string JobName { get; set; } = "";
    public string Material { get; set; } = "";
    public string Plate { get; set; } = "";
    public double Progress { get; set; }            // 0..1
    public int LayerCurrent { get; set; }
    public int LayerTotal { get; set; }
    public double SecondsLeft { get; set; }
    public double NozzleC { get; set; }
    public double NozzleTargetC { get; set; }
    public double BedC { get; set; }
    public double BedTargetC { get; set; }
    public double? ChamberC { get; set; }
    public double FanPartPct { get; set; }
    public double FanAuxPct { get; set; }
    public PrintSpeed Speed { get; set; } = PrintSpeed.Standard;
    public bool LightOn { get; set; }
    public string FilamentColorHex { get; set; } = "#E69F00";
    public string ErrorText { get; set; } = "";
    public List<AmsUnit> AmsUnits { get; set; } = new();
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    public PrinterState Clone() => (PrinterState)MemberwiseClone();
}
