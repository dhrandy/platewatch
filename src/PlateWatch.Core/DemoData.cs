namespace PlateWatch.Core;

/// <summary>The canned demo fleet from the approved design renders.</summary>
public static class DemoData
{
    public static Fleet BuildFleet(DateTimeOffset? start = null)
    {
        var p1s = new MockBambuClient(new MockPrinterSpec
        {
            Identity = new PrinterIdentity("P1S (demo)", "Bambu Lab P1S", "DEMO-P1S-001",
                                           "192.168.1.51", "01.03.00.00"),
            Status = PrinterStatus.Printing,
            JobName = "Articulated Dragon",
            Material = "PLA Silk+",
            Plate = "A4",
            InitialProgress = 0.63,
            LayerTotal = 284,
            TotalSeconds = 3540.0 / (1 - 0.63),  // so 63% done = 59m left
            NozzleC = 220, BedC = 55, ChamberC = 38,
            AmsUnits = new List<AmsUnit>
            {
                new()
                {
                    Name = "AMS 2 PRO A", HumidityPct = 16, TemperatureC = 28,
                    Spools = new List<Spool>
                    {
                        new() { Slot = 1, ColorHex = "#E69F00", Material = "PLA", Remaining01 = 0.82, Active = true },
                        new() { Slot = 2, ColorHex = "#56B4E9", Material = "PLA", Remaining01 = 0.64 },
                        new() { Slot = 3, ColorHex = "#CC79A7", Material = "PETG", Remaining01 = 0.31 },
                        new() { Slot = 4, ColorHex = "#F0E442", Material = "PLA", Remaining01 = 0.12 },
                    },
                },
            },
        }, start);

        var a1 = new MockBambuClient(new MockPrinterSpec
        {
            Identity = new PrinterIdentity("A1 mini (demo)", "Bambu Lab A1 mini", "DEMO-A1M-002",
                                           "192.168.1.52", "01.02.01.00"),
            Status = PrinterStatus.Idle,
        }, start);

        var x1c = new MockBambuClient(new MockPrinterSpec
        {
            Identity = new PrinterIdentity("X1C (demo)", "Bambu Lab X1 Carbon", "DEMO-X1C-003",
                                           "192.168.1.53", "01.06.05.00"),
            Status = PrinterStatus.Finished,
            ChamberC = 40,
            JobName = "Planter",
            Material = "PETG",
            InitialProgress = 1.0,
            LayerTotal = 412,
        }, start);

        return new Fleet(new IBambuClient[] { p1s, a1, x1c });
    }
}
