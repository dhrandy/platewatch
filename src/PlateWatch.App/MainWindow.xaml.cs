using Microsoft.UI.Xaml;
using PlateWatch.App.Services;
using Microsoft.UI.Xaml.Media;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App;

/// <summary>
/// The full window: fleet chips across the top, hero card for the selected
/// printer, temps and filament cards on the right. Matches the approved
/// c1v2 window render.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow(FleetViewModel fleet)
    {
        Fleet = fleet;
        InitializeComponent();
        Title = "PlateWatch";
        SystemBackdrop = new MicaBackdrop();

        var appWindow = AppWindow;
        if (IconFile.CurrentPath is not null)
            appWindow.SetIcon(IconFile.CurrentPath);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));
        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 860;
            presenter.PreferredMinimumHeight = 620;
        }

        Closed += (_, _) => Application.Current.Exit();
    }

    public FleetViewModel Fleet { get; }
}
