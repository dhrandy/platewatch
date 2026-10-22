namespace PlateWatch.Core;

/// <summary>One status style: color + glyph + word. Status is never color alone.</summary>
public sealed record StatusStyle(string ColorHex, string Glyph, string Label);

/// <summary>
/// Design tokens from the approved concept render. Status colors are the
/// Okabe-Ito colorblind-safe palette; the single UI accent is Windows blue.
/// </summary>
public static class DesignTokens
{
    public const string Accent = "#4CC2FF";
    public const string Warning = "#E69F00";   // amber: the one warning color
    public const string WindowTop = "#212121";
    public const string WindowBottom = "#1B1B1B";
    public const string CardFill = "#FFFFFF0B";
    public const string CardBorder = "#FFFFFF12";
    public const string TextPrimary = "#F3F4F6";
    public const string TextSecondary = "#A7ABB3";
    public const string TextDim = "#8A919C";

    private static readonly IReadOnlyDictionary<PrinterStatus, StatusStyle> Styles =
        new Dictionary<PrinterStatus, StatusStyle>
        {
            [PrinterStatus.Printing] = new("#0072B2", "●", "Printing"),        // blue
            [PrinterStatus.Paused]   = new("#E69F00", "❚❚", "Paused"),         // amber
            [PrinterStatus.Finished] = new("#009E73", "✓", "Finished"),        // bluish green
            [PrinterStatus.Error]    = new("#D55E00", "!", "Needs attention"), // vermillion
            [PrinterStatus.Idle]     = new("#9AA0A6", "○", "Idle"),            // neutral gray
        };

    public static StatusStyle StyleFor(PrinterStatus status) => Styles[status];

    public static IReadOnlyDictionary<PrinterStatus, StatusStyle> AllStyles => Styles;
}
