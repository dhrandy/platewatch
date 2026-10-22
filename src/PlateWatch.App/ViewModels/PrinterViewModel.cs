using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using PlateWatch.Core;

namespace PlateWatch.App.ViewModels;

public partial class PrinterViewModel : ObservableObject
{
    private readonly IBambuClient _client;

    public PrinterViewModel(IBambuClient client)
    {
        _client = client;
        _client.StateChanged += (_, _) => Refresh();
        Refresh();
    }

    public IBambuClient Client => _client;

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _statusLabel = "";
    [ObservableProperty] private string _statusGlyph = "";
    [ObservableProperty] private Brush _statusBrush = new SolidColorBrush(Colors.Gray);
    [ObservableProperty] private Brush _statusPillBrush = new SolidColorBrush(Colors.Transparent);
    [ObservableProperty] private string _jobName = "";
    [ObservableProperty] private string _materialLine = "";
    [ObservableProperty] private string _percentText = "0";
    [ObservableProperty] private string _layersText = "";
    [ObservableProperty] private string _timeLeftText = "";
    [ObservableProperty] private string _finishText = "";
    [ObservableProperty] private double _progress01;
    [ObservableProperty] private double _progressPct;
    [ObservableProperty] private Brush _filamentBrush = new SolidColorBrush(Colors.Gray);
    [ObservableProperty] private string _nozzleText = "";
    [ObservableProperty] private string _nozzleSub = "";
    [ObservableProperty] private string _bedText = "";
    [ObservableProperty] private string _bedSub = "";
    [ObservableProperty] private string _chamberText = "";
    [ObservableProperty] private string _fansText = "";
    [ObservableProperty] private string _fansSub = "";
    [ObservableProperty] private string _amsTitle = "";
    [ObservableProperty] private string _amsEnvText = "";
    [ObservableProperty] private string _activeSpoolLine = "";
    [ObservableProperty] private string _pauseLabel = "Pause";
    [ObservableProperty] private bool _canPause;
    [ObservableProperty] private string _speedLabel = "Speed";
    [ObservableProperty] private string _lightLabel = "Light";
    [ObservableProperty] private double _nozzleFrac01;
    [ObservableProperty] private double _bedFrac01;
    [ObservableProperty] private double _fanFrac01;
    [ObservableProperty] private string _filamentHeader = "FILAMENT";
    [ObservableProperty] private string _activeSpoolCaption = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChipBackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(ChipBorderBrush))]
    [NotifyPropertyChangedFor(nameof(ChipNameBrush))]
    [NotifyPropertyChangedFor(nameof(ChipSubBrush))]
    private bool _isSelected;

    /// <summary>Set by FleetViewModel so a chip tap can select this printer.</summary>
    public Action? RequestSelect { get; set; }

    [RelayCommand]
    private void Select() => RequestSelect?.Invoke();

    private static readonly Brush ChipSelBg = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF));
    private static readonly Brush ChipSelBorder = new SolidColorBrush(Color.FromArgb(0x2E, 0xFF, 0xFF, 0xFF));
    private static readonly Brush Clear = new SolidColorBrush(Colors.Transparent);
    private static readonly Brush ChipNameSel = new SolidColorBrush(SpoolViewModel.Parse("#F3F4F6"));
    private static readonly Brush ChipNameIdle = new SolidColorBrush(SpoolViewModel.Parse("#C9CDD4"));
    private static readonly Brush ChipSubIdle = new SolidColorBrush(SpoolViewModel.Parse("#8A919C"));

    public Brush ChipBackgroundBrush => IsSelected ? ChipSelBg : Clear;
    public Brush ChipBorderBrush => IsSelected ? ChipSelBorder : Clear;
    public Brush ChipNameBrush => IsSelected ? ChipNameSel : ChipNameIdle;
    public Brush ChipSubBrush => IsSelected ? StatusBrush : ChipSubIdle;

    public string ChipLine => _client.State.Status is PrinterStatus.Printing or PrinterStatus.Paused
        ? $"{StatusLabel} · {PercentText}%"
        : StatusLabel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StopLabel))]
    [NotifyPropertyChangedFor(nameof(StopBrush))]
    [NotifyPropertyChangedFor(nameof(StopForegroundBrush))]
    private bool _stopArmed;

    public string StopLabel => StopArmed ? "Sure?" : "Stop";

    public List<SpoolViewModel> Spools { get; private set; } = new();

    private static readonly Brush NeutralBtn = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
    private static readonly Brush ArmedBtn = new SolidColorBrush(SpoolViewModel.Parse("#D55E00"));
    private static readonly Brush StopText = new SolidColorBrush(SpoolViewModel.Parse("#E88B5C"));
    private static readonly Brush ArmedText = new SolidColorBrush(Colors.White);

    public Brush StopBrush => StopArmed ? ArmedBtn : NeutralBtn;
    public Brush StopForegroundBrush => StopArmed ? ArmedText : StopText;

    [RelayCommand]
    private async Task PauseResumeAsync()
    {
        if (_client.State.Status == PrinterStatus.Paused) await _client.ResumeAsync();
        else await _client.PauseAsync();
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        if (!StopArmed)
        {
            StopArmed = true;
            await Task.Delay(3000);
            StopArmed = false;
            return;
        }
        await _client.StopAsync();
        StopArmed = false;
    }

    [RelayCommand]
    private async Task CycleSpeedAsync()
    {
        var speeds = Enum.GetValues<PrintSpeed>();
        var next = speeds[(Array.IndexOf(speeds, _client.State.Speed) + 1) % speeds.Length];
        await _client.SetSpeedAsync(next);
    }

    [RelayCommand]
    private async Task ToggleLightAsync()
        => await _client.SetLightAsync(!_client.State.LightOn);

    public void Refresh()
    {
        var s = _client.State;
        var style = DesignTokens.StyleFor(s.Status);
        Name = _client.Identity.Name;
        StatusLabel = style.Label;
        StatusGlyph = style.Glyph;
        StatusBrush = new SolidColorBrush(SpoolViewModel.Parse(style.ColorHex));
        var pillColor = SpoolViewModel.Parse(style.ColorHex);
        pillColor.A = 0x3C;
        StatusPillBrush = new SolidColorBrush(pillColor);
        JobName = s.JobName;
        MaterialLine = string.Join("  ·  ", new[] { s.Material, s.Plate, $"{s.LayerCurrent}/{s.LayerTotal} layers" }
            .Where(p => !string.IsNullOrEmpty(p)));
        PercentText = $"{s.Progress * 100:0}";
        LayersText = $"{s.LayerCurrent}/{s.LayerTotal} layers";
        Progress01 = s.Progress;
        ProgressPct = s.Progress * 100;
        FilamentBrush = new SolidColorBrush(SpoolViewModel.Parse(s.FilamentColorHex));
        NozzleText = $"{s.NozzleC:0}°";
        NozzleSub = $"at {s.NozzleTargetC:0}°";
        BedText = $"{s.BedC:0}°";
        BedSub = $"at {s.BedTargetC:0}°";
        ChamberText = s.ChamberC is null ? "-" : $"{s.ChamberC:0}°";
        FansText = $"{s.FanPartPct:0}%";
        FansSub = $"part · aux {s.FanAuxPct:0}%";
        NozzleFrac01 = s.NozzleTargetC > 0 ? Math.Clamp(s.NozzleC / s.NozzleTargetC, 0, 1) : 0;
        BedFrac01 = s.BedTargetC > 0 ? Math.Clamp(s.BedC / s.BedTargetC, 0, 1) : 0;
        FanFrac01 = Math.Clamp(s.FanPartPct / 100.0, 0, 1);
        PauseLabel = s.Status == PrinterStatus.Paused ? "Resume" : "Pause";
        CanPause = s.Status is PrinterStatus.Printing or PrinterStatus.Paused;
        SpeedLabel = $"Speed: {s.Speed}";
        LightLabel = s.LightOn ? "Light: On" : "Light: Off";

        if (s.Status is PrinterStatus.Printing or PrinterStatus.Paused)
        {
            TimeLeftText = Formatting.FormatTimeLeft(s.SecondsLeft) + " left";
            FinishText = Formatting.FormatFinish(DateTimeOffset.Now, s.SecondsLeft);
        }
        else if (s.Status == PrinterStatus.Finished)
        {
            TimeLeftText = "Print finished";
            FinishText = "";
        }
        else
        {
            TimeLeftText = "Nothing printing";
            FinishText = "";
        }

        var ams = s.AmsUnits.FirstOrDefault();
        if (ams is not null)
        {
            AmsTitle = ams.Name.ToUpperInvariant();
            FilamentHeader = $"FILAMENT · {AmsTitle}";
            AmsEnvText = ams.HumidityPct is null ? "" : $"{ams.HumidityPct:0}% RH · {ams.TemperatureC:0}°";
            Spools = ams.Spools.Select(sp => new SpoolViewModel(sp)).ToList();
            var active = ams.Spools.FirstOrDefault(sp => sp.Active);
            ActiveSpoolLine = active is null ? "" :
                $"{active.Material} · {active.Remaining01 * 100:0}% left";
            ActiveSpoolCaption = active is null ? "" :
                $"Spool {active.Slot} · {active.Material} · active";
        }
        OnPropertyChanged(nameof(Spools));
        OnPropertyChanged(nameof(ChipLine));
    }
}
