using Microsoft.UI.Xaml;

namespace PlateWatch.App.Controls;

public static class Converters
{
    public static Visibility BoolToVis(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
}
