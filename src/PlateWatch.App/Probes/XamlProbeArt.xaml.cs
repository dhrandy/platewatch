using Microsoft.UI.Xaml;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App.Probes;

public sealed partial class XamlProbeArt : Window
{
    public XamlProbeArt(FleetViewModel fleet)
    {
        Fleet = fleet;
        InitializeComponent();
    }

    public FleetViewModel Fleet { get; }
}
