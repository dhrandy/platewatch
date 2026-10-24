using Windows.UI;
using PlateWatch.Core;

namespace PlateWatch.App.Services;

/// <summary>
/// Software-renders tray icons: dark rounded tile, progress ring arc in the
/// status color, center dot. Okabe-Ito palette, same as the app. No asset
/// files needed and the ring can show live progress.
/// </summary>
public static class TrayIconRenderer
{
    public const int Size = 64;

    public static byte[] RenderPixels(PrinterStatus status, double progress01, string colorHex)
    {
        var color = Parse(colorHex);
        var track = Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF);
        var bg = Color.FromArgb(0xFF, 0x1B, 0x1B, 0x1B);
        var sweep = Math.Clamp(progress01, 0, 1) * 360.0;

        var pixels = new byte[Size * Size * 4];
        Span<byte> row = stackalloc byte[Size * 4];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                Color? c = null;
                if (InsideRoundedSquare(x, y, Size, 14))
                {
                    c = bg;
                    var dx = x + 0.5 - Size / 2.0;
                    var dy = y + 0.5 - Size / 2.0;
                    var r = Math.Sqrt(dx * dx + dy * dy);
                    if (r is >= 22 and <= 30)
                    {
                        // angle measured clockwise from 12 o'clock
                        var ang = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
                        if (ang < 0) ang += 360;
                        c = ang <= sweep ? color : track;
                    }
                    else if (r <= 11)
                    {
                        c = color;
                    }
                }
                WritePixel(row, x * 4, c ?? Color.FromArgb(0, 0, 0, 0));
            }
            row.CopyTo(pixels.AsSpan(y * Size * 4));
        }
        return pixels;
    }

    private static bool InsideRoundedSquare(double x, double y, int size, double radius)
    {
        var cx = Math.Clamp(x, radius, size - 1 - radius);
        var cy = Math.Clamp(y, radius, size - 1 - radius);
        var dx = x - cx;
        var dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static void WritePixel(Span<byte> buf, int i, Color c)
    {
        // WriteableBitmap is BGRA premultiplied
        var a = c.A / 255.0;
        buf[i] = (byte)(c.B * a);
        buf[i + 1] = (byte)(c.G * a);
        buf[i + 2] = (byte)(c.R * a);
        buf[i + 3] = c.A;
    }

    private static Color Parse(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(255,
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }
}
