using Microsoft.UI.Xaml;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App.Probes;

public sealed partial class XamlProbeTemps : Window
{
    public XamlProbeTemps(FleetViewModel fleet)
    {
        Fleet = fleet;
        InitializeComponent();
    }

    public FleetViewModel Fleet { get; }
}
