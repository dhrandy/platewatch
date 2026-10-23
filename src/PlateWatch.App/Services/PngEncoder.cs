using System.IO.Compression;

namespace PlateWatch.App.Services;

/// <summary>
/// Minimal PNG encoder (RGBA8, filter 0, zlib via ZLibStream) so icon
/// generation needs no WinRT and can run synchronously before any window
/// exists. The previous WinRT BitmapEncoder path deadlocked the UI thread
/// at startup when blocked on with GetAwaiter().GetResult().
/// </summary>
internal static class PngEncoder
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Encode premultiplied BGRA pixels (WriteableBitmap layout, as produced
    /// by TrayIconRenderer.RenderPixels) as a straight-alpha RGBA PNG.
    /// </summary>
    public static byte[] EncodePremultipliedBgra(byte[] bgra, int width, int height)
    {
        var stride = width * 4;
        var raw = new byte[height * (stride + 1)];
        for (var y = 0; y < height; y++)
        {
            var dstRow = y * (stride + 1);
            raw[dstRow] = 0; // filter: none
            for (var x = 0; x < width; x++)
            {
                var src = y * stride + x * 4;
                var a = bgra[src + 3];
                var dst = dstRow + 1 + x * 4;
                if (a == 0)
                {
                    raw[dst] = 0;
                    raw[dst + 1] = 0;
                    raw[dst + 2] = 0;
                    raw[dst + 3] = 0;
                }
                else
                {
                    // unpremultiply back to straight alpha
                    raw[dst] = (byte)Math.Min(255, bgra[src + 2] * 255 / a);     // R
                    raw[dst + 1] = (byte)Math.Min(255, bgra[src + 1] * 255 / a); // G
                    raw[dst + 2] = (byte)Math.Min(255, bgra[src] * 255 / a);     // B
                    raw[dst + 3] = a;
                }
            }
        }

        byte[] idat;
        using (var compressed = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
                zlib.Write(raw, 0, raw.Length);
            idat = compressed.ToArray();
        }

        using var png = new MemoryStream();
        png.Write(Signature, 0, Signature.Length);
        WriteChunk(png, "IHDR", ms =>
        {
            WriteBE(ms, (uint)width);
            WriteBE(ms, (uint)height);
            ms.WriteByte(8); // bit depth
            ms.WriteByte(6); // color type: RGBA
            ms.WriteByte(0); // compression
            ms.WriteByte(0); // filter
            ms.WriteByte(0); // interlace
        });
        WriteChunk(png, "IDAT", ms => ms.Write(idat, 0, idat.Length));
        WriteChunk(png, "IEND", _ => { });
        return png.ToArray();
    }

    private static void WriteChunk(Stream output, string type, Action<MemoryStream> writeData)
    {
        using var data = new MemoryStream();
        writeData(data);
        var bytes = data.ToArray();
        WriteBE(output, (uint)bytes.Length);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes, 0, typeBytes.Length);
        output.Write(bytes, 0, bytes.Length);
        WriteBE(output, Crc32(typeBytes, bytes));
    }

    private static void WriteBE(Stream s, uint v)
    {
        s.WriteByte((byte)(v >> 24));
        s.WriteByte((byte)(v >> 16));
        s.WriteByte((byte)(v >> 8));
        s.WriteByte((byte)v);
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in type) crc = CrcStep(crc, b);
        foreach (var b in data) crc = CrcStep(crc, b);
        return crc ^ 0xFFFFFFFFu;
    }

    private static uint CrcStep(uint crc, byte b)
    {
        crc ^= b;
        for (var k = 0; k < 8; k++)
            crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        return crc;
    }
}
