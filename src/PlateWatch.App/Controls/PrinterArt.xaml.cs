using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App.Controls;

public sealed partial class PrinterArt : UserControl
{
    public PrinterArt()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdatePart();
    }

    public static readonly DependencyProperty SpoolsProperty =
        DependencyProperty.Register(nameof(Spools), typeof(IEnumerable<SpoolViewModel>),
            typeof(PrinterArt), new PropertyMetadata(new List<SpoolViewModel>()));
    public IEnumerable<SpoolViewModel> Spools
    {
        get => (IEnumerable<SpoolViewModel>)GetValue(SpoolsProperty);
        set => SetValue(SpoolsProperty, value);
    }

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double),
            typeof(PrinterArt), new PropertyMetadata(0.0, (d, _) => ((PrinterArt)d).UpdatePart()));
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public static readonly DependencyProperty FilamentBrushProperty =
        DependencyProperty.Register(nameof(FilamentBrush), typeof(Brush),
            typeof(PrinterArt), new PropertyMetadata(new SolidColorBrush(Microsoft.UI.Colors.Orange)));
    public Brush FilamentBrush
    {
        get => (Brush)GetValue(FilamentBrushProperty);
        set => SetValue(FilamentBrushProperty, value);
    }

    // read by x:Bind for the part rectangle
    public double PartHeight { get; private set; }

    private void UpdatePart()
    {
        // the part grows up from the plate inside the glass door
        var doorHeight = Door.ActualHeight;
        var maxH = Math.Max(0, doorHeight - 42); // gantry + plate margins
        PartHeight = Math.Max(0, maxH * Math.Clamp(Progress, 0, 1));
        Bindings.Update();
    }
}
