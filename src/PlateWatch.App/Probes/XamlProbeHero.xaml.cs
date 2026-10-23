using Microsoft.UI.Xaml;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App.Probes;

public sealed partial class XamlProbeHero : Window
{
    public XamlProbeHero(FleetViewModel fleet)
    {
        Fleet = fleet;
        InitializeComponent();
    }

    public FleetViewModel Fleet { get; }
}
