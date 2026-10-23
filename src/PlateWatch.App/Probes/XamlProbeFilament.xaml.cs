using Microsoft.UI.Xaml;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App.Probes;

public sealed partial class XamlProbeFilament : Window
{
    public XamlProbeFilament(FleetViewModel fleet)
    {
        Fleet = fleet;
        InitializeComponent();
    }

    public FleetViewModel Fleet { get; }
}
