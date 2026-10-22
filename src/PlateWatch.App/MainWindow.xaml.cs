using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace PlateWatch.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "PlateWatch";
        SystemBackdrop = new MicaBackdrop();
    }
}
