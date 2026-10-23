using Microsoft.UI.Xaml;
using PlateWatch.App.Services;
using Microsoft.UI.Xaml.Media;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App;

/// <summary>
/// The borderless tray popup. Positioned above the taskbar corner, hides when
/// it loses focus, mica backdrop like the approved render.
/// </summary>
public sealed partial class PopupWindow : Window
{
    public PopupWindow(PrinterViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Title = "PlateWatch";
        SystemBackdrop = new MicaBackdrop();

        var appWindow = AppWindow;
        if (IconFile.CurrentPath is not null)
            appWindow.SetIcon(IconFile.CurrentPath);
        appWindow.Resize(new Windows.Graphics.SizeInt32(408, 620));
        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }
        appWindow.IsShownInSwitchers = false;

        Activated += (_, args) =>
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
                appWindow.Hide();
        };
    }

    public PrinterViewModel ViewModel { get; }

    public void ShowAtTray()
    {
        var appWindow = AppWindow;
        var area = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(appWindow.Id,
            Microsoft.UI.Windowing.DisplayAreaFallback.Primary).WorkArea;
        appWindow.Move(new Windows.Graphics.PointInt32(
            area.Width - appWindow.Size.Width - 12,
            area.Height - appWindow.Size.Height - 12));
        appWindow.Show();
        Activate();
    }
}
