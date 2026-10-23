using Microsoft.UI.Xaml;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App.Probes;

public sealed partial class XamlProbeHeader : Window
{
    public XamlProbeHeader(FleetViewModel fleet)
    {
        Fleet = fleet;
        InitializeComponent();
    }

    public FleetViewModel Fleet { get; }
}
