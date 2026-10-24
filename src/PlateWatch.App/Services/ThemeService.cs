using Microsoft.UI.Xaml;

namespace PlateWatch.App.Services;

/// <summary>
/// App-wide theme mode: Dark (the approved design, default), Light (same
/// tokens, deliberately adapted), or System (follow Windows). Applies per
/// window via the content's RequestedTheme so it can switch at runtime;
/// windows and theme-dependent brushes listen to Changed.
/// </summary>
public static class ThemeService
{
    public static ElementTheme Current { get; private set; } = ElementTheme.Dark;
    public static bool IsLight { get; private set; }
    public static event EventHandler? Changed;

    public static void Apply(string? setting)
    {
        Current = setting switch
        {
            "Light" => ElementTheme.Light,
            "System" => ElementTheme.Default,
            _ => ElementTheme.Dark,
        };
        IsLight = Current switch
        {
            ElementTheme.Light => true,
            ElementTheme.Dark => false,
            _ => SystemIsLight(),
        };
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void ApplyTo(Window window)
    {
        if (window.Content is FrameworkElement content)
            content.RequestedTheme = Current;
    }

    private static bool SystemIsLight()
    {
        var ui = new Windows.UI.ViewManagement.UISettings();
        var bg = ui.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        return bg.R > 127;
    }
}
