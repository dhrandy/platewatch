using Windows.Graphics.Imaging;
using PlateWatch.Core;

namespace PlateWatch.App.Services;

/// <summary>
/// Writes the app icon (the tray-tile design, accent ring) to a real .ico
/// under %APPDATA%/PlateWatch at startup, so windows and the taskbar get the
/// icon without shipping binary assets in the repo. ICO wraps a PNG image.
/// </summary>
public static class IconFile
{
    public static string? CurrentPath { get; private set; }

    public static async Task EnsureAsync()
    {
        if (CurrentPath is not null) return;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlateWatch");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "icon.ico");

        var pixels = TrayIconRenderer.RenderPixels(PrinterStatus.Printing, 0.66, DesignTokens.Accent);
        const int size = TrayIconRenderer.Size;

        using var pngStream = new MemoryStream();
        var encoder = await BitmapEncoder.CreateAsync(
            BitmapEncoder.PngEncoderId, pngStream.AsRandomAccessStream());
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
            size, size, 96, 96, pixels);
        await encoder.FlushAsync();
        var png = pngStream.ToArray();

        using var ico = new BinaryWriter(File.Create(path));
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

        CurrentPath = path;
    }
}
