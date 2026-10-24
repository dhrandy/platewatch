using PlateWatch.Core;

namespace PlateWatch.App.Services;

/// <summary>
/// Writes the app icon (the tray-tile design, accent ring) to a real .ico
/// under %APPDATA%/PlateWatch at startup, so windows and the taskbar get the
/// icon without shipping binary assets in the repo. ICO wraps a PNG image.
/// Synchronous and pure managed: the first version used WinRT's
/// BitmapEncoder blocked with GetAwaiter().GetResult() on the UI thread,
/// which deadlocked before any window or tray icon could appear (process
/// stayed alive as a background process with zero UI).
/// </summary>
public static class IconFile
{
    public static string? CurrentPath { get; private set; }

    public static void Ensure()
    {
        if (CurrentPath is not null) return;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlateWatch");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "icon.ico");

        var pixels = TrayIconRenderer.RenderPixels(PrinterStatus.Printing, 0.66, DesignTokens.Accent);
        File.WriteAllBytes(path, EncodeIco(pixels, TrayIconRenderer.Size));

        CurrentPath = path;
    }

    /// <summary>Wraps premultiplied BGRA pixels (TrayIconRenderer.RenderPixels) as a single-image ICO.</summary>
    public static byte[] EncodeIco(byte[] bgraPremultiplied, int size)
    {
        var png = PngEncoder.EncodePremultipliedBgra(bgraPremultiplied, size, size);
        using var ms = new MemoryStream();
        using var ico = new BinaryWriter(ms);
        ico.Write((ushort)0);          // reserved
        ico.Write((ushort)1);          // icon type
        ico.Write((ushort)1);          // image count
        ico.Write((byte)size);         // width (64)
        ico.Write((byte)size);         // height
        ico.Write((byte)0);            // palette
        ico.Write((byte)0);            // reserved
        ico.Write((ushort)1);          // planes
        ico.Write((ushort)32);         // bpp
        ico.Write((uint)png.Length);   // image size
        ico.Write((uint)22);           // image offset
        ico.Write(png);
        ico.Flush();
        return ms.ToArray();
    }
}
