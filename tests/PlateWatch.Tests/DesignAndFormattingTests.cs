using PlateWatch.Core;

namespace PlateWatch.Tests;

public class DesignAndFormattingTests
{
    [Fact]
    public void Every_status_has_color_glyph_and_word()
    {
        foreach (var status in Enum.GetValues<PrinterStatus>())
        {
            var style = DesignTokens.StyleFor(status);
            Assert.False(string.IsNullOrWhiteSpace(style.ColorHex), $"{status} needs a color");
            Assert.False(string.IsNullOrWhiteSpace(style.Glyph), $"{status} needs a glyph");
            Assert.False(string.IsNullOrWhiteSpace(style.Label), $"{status} needs a word");
        }
    }

    [Fact]
    public void Status_glyphs_and_colors_are_unique()
    {
        Assert.Equal(6, DesignTokens.AllStyles.Values.Select(s => s.Glyph).Distinct().Count());
        Assert.Equal(6, DesignTokens.AllStyles.Values.Select(s => s.ColorHex).Distinct().Count());
    }

    [Fact]
    public void Status_palette_is_okabe_ito_colorblind_safe()
    {
        // Okabe-Ito set plus the two neutral grays (idle, offline) - both colorblind-safe
        var allowed = new[] { "#0072B2", "#E69F00", "#009E73", "#D55E00", "#9AA0A6", "#6B7280" };
        foreach (var style in DesignTokens.AllStyles.Values)
            Assert.Contains(style.ColorHex, allowed);
    }

    [Fact]
    public void Time_left_formats_like_the_render()
    {
        Assert.Equal("59m", Formatting.FormatTimeLeft(3540));
        Assert.Equal("1h 01m", Formatting.FormatTimeLeft(3660));
        Assert.Equal("0m", Formatting.FormatTimeLeft(0));
    }

    [Fact]
    public void Finish_time_formats_like_the_render()
    {
        var t = new DateTimeOffset(2026, 10, 22, 9, 41, 0, TimeSpan.Zero);
        Assert.Equal("Done 10:40", Formatting.FormatFinish(t, 59 * 60));
    }
}
