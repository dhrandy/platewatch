using Microsoft.UI.Xaml;
using PlateWatch.App.Services;
using PlateWatch.App.ViewModels;

namespace PlateWatch.App.Probes;

/// <summary>
/// CI diagnostics: parse each section of the real MainWindow layout as its
/// own window so a Release-mode XamlParseException (which carries no line
/// info) can be narrowed to the exact construct. Run only when the
/// PLATEWATCH_XAML_PROBE env var is "1" (the CI smoke test sets it).
/// </summary>
internal static class XamlProbe
{
    public static void Run(FleetViewModel fleet)
    {
        Probe("shell", () => new XamlProbeShell(fleet));
        Probe("header", () => new XamlProbeHeader(fleet));
        Probe("hero-noart", () => new XamlProbeHero(fleet));
        Probe("temps", () => new XamlProbeTemps(fleet));
        Probe("filament", () => new XamlProbeFilament(fleet));
        Probe("printerart", () => new XamlProbeArt(fleet));
    }

    private static void Probe(string name, Func<Window> make)
    {
        try
        {
            _ = make();
            StartupLog.Step("xamlprobe " + name + " ok");
        }
        catch (Exception ex)
        {
            StartupLog.Step("xamlprobe " + name + " FAIL " + ex.GetType().FullName + ": " + ex.Message);
        }
    }
}
