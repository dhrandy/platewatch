using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using PlateWatch.Core;
using PlateWatch.App.Services;

namespace PlateWatch.App.ViewModels;

public partial class PrinterViewModel : ObservableObject
{
    private readonly IBambuClient _client;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _ui =
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    public PrinterViewModel(IBambuClient client)
    {
        _client = client;
        // real printers raise StateChanged from MQTT threads; view updates
        // must happen on the UI thread
        _client.StateChanged += (_, _) =>
        {
            if (_ui.HasThreadAccess) Refresh();
            else _ui.TryEnqueue(() => Refresh());
        };
        ThemeService.Changed += (_, _) =>
        {
            if (_ui.HasThreadAccess) RefreshThemeBrushes();
            else _ui.TryEnqueue(() => RefreshThemeBrushes());
        };
        Refresh();
    }

    private void RefreshThemeBrushes()
    {
        StatusBrush = new SolidColorBrush(SpoolViewModel.Parse(
            StatusColorHex(DesignTokens.StyleFor(_client.State.Status))));
        OnPropertyChanged(nameof(ChipBackgroundBrush));
        OnPropertyChanged(nameof(ChipBorderBrush));
        OnPropertyChanged(nameof(ChipNameBrush));
        OnPropertyChanged(nameof(ChipSubBrush));
        OnPropertyChanged(nameof(StopBrush));
        OnPropertyChanged(nameof(StopForegroundBrush));
    }

    // Dark theme uses the approved Okabe-Ito colors as-is; on white cards
    // several fail 4.5:1 normal-text contrast, so light uses darkened
    // variants of the same hues (measured: 4.6-6.2:1 on #FFFFFF).
    private static string StatusColorHex(StatusStyle style) =>
        !ThemeService.IsLight ? style.ColorHex : style.ColorHex switch
        {
            "#E69F00" => "#A66100", // paused amber
            "#009E73" => "#007A5A", // finished green
            "#D55E00" => "#C45000", // error vermillion
            "#9AA0A6" => "#5F6368", // idle gray
            _ => style.ColorHex,    // printing blue, offline gray pass as-is
        };

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
    [NotifyPropertyChangedFor(nameof(FansColumnSpan))]
    [NotifyPropertyChangedFor(nameof(PopupFansColumn))]
    [NotifyPropertyChangedFor(nameof(PopupFansColumnSpan))]
    private bool _hasChamber = true;

    [ObservableProperty] private bool _hasAms;

    // layout: without a chamber card the fans card takes its space
    public int FansColumnSpan => HasChamber ? 1 : 2;
    public int PopupFansColumn => HasChamber ? 3 : 2;
    public int PopupFansColumnSpan => HasChamber ? 1 : 2;

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

    private static readonly Brush ChipSelBgD = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF));
    private static readonly Brush ChipSelBgL = new SolidColorBrush(Color.FromArgb(0x0F, 0x00, 0x00, 0x00));
    private static readonly Brush ChipSelBorderD = new SolidColorBrush(Color.FromArgb(0x2E, 0xFF, 0xFF, 0xFF));
    private static readonly Brush ChipSelBorderL = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00));
    private static readonly Brush Clear = new SolidColorBrush(Colors.Transparent);
    private static readonly Brush ChipNameSelD = new SolidColorBrush(SpoolViewModel.Parse("#F3F4F6"));
    private static readonly Brush ChipNameSelL = new SolidColorBrush(SpoolViewModel.Parse("#1B1B1B"));
    private static readonly Brush ChipNameIdleD = new SolidColorBrush(SpoolViewModel.Parse("#C9CDD4"));
    private static readonly Brush ChipNameIdleL = new SolidColorBrush(SpoolViewModel.Parse("#3C4043"));
    private static readonly Brush ChipSubIdleD = new SolidColorBrush(SpoolViewModel.Parse("#8A919C"));
    private static readonly Brush ChipSubIdleL = new SolidColorBrush(SpoolViewModel.Parse("#5F6368"));

    public Brush ChipBackgroundBrush => IsSelected
        ? (ThemeService.IsLight ? ChipSelBgL : ChipSelBgD) : Clear;
    public Brush ChipBorderBrush => IsSelected
        ? (ThemeService.IsLight ? ChipSelBorderL : ChipSelBorderD) : Clear;
    public Brush ChipNameBrush => IsSelected
        ? (ThemeService.IsLight ? ChipNameSelL : ChipNameSelD)
        : (ThemeService.IsLight ? ChipNameIdleL : ChipNameIdleD);
    public Brush ChipSubBrush => IsSelected ? StatusBrush
        : (ThemeService.IsLight ? ChipSubIdleL : ChipSubIdleD);

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

    private static readonly Brush NeutralBtnD = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
    private static readonly Brush NeutralBtnL = new SolidColorBrush(Color.FromArgb(0x0F, 0x00, 0x00, 0x00));
    private static readonly Brush ArmedBtn = new SolidColorBrush(SpoolViewModel.Parse("#D55E00"));
    private static readonly Brush StopTextD = new SolidColorBrush(SpoolViewModel.Parse("#E88B5C"));
    private static readonly Brush StopTextL = new SolidColorBrush(SpoolViewModel.Parse("#C45000"));
    private static readonly Brush ArmedText = new SolidColorBrush(Colors.White);

    public Brush StopBrush => StopArmed ? ArmedBtn
        : (ThemeService.IsLight ? NeutralBtnL : NeutralBtnD);
    public Brush StopForegroundBrush => StopArmed ? ArmedText
        : (ThemeService.IsLight ? StopTextL : StopTextD);

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
        StatusBrush = new SolidColorBrush(SpoolViewModel.Parse(StatusColorHex(style)));
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
        HasChamber = s.ChamberC.HasValue;
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
            TimeLeftText = s.Status == PrinterStatus.Offline ? "Printer offline" : "Nothing printing";
            FinishText = "";
        }

        var ams = s.AmsUnits.FirstOrDefault();
        HasAms = ams is not null;
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
