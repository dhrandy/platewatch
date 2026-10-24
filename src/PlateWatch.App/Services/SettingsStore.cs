using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PlateWatch.App.Services;

public sealed class PrinterConfig
{
    public string Name { get; set; } = "";
    public string Model { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string Serial { get; set; } = "";
    /// <summary>DPAPI-protected (current user), base64. Never plaintext on disk.</summary>
    public string AccessCodeProtected { get; set; } = "";
}

public sealed class AppSettings
{
    public bool ToastsEnabled { get; set; } = true;
    public bool ToastOnPrintStart { get; set; }   // opt-in, per the approved plan
    public bool StartWithWindows { get; set; }
    public int WindowWidth { get; set; } = 1000;
    public int WindowHeight { get; set; } = 700;
    public List<PrinterConfig> Printers { get; set; } = new();
}

/// <summary>
/// JSON settings in %APPDATA%/PlateWatch. The printer access code is stored
/// DPAPI-protected for the current user, never as plaintext.
/// </summary>
public sealed class SettingsStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlateWatch");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Settings { get; private set; } = new();

    public static SettingsStore Load()
    {
        var store = new SettingsStore();
        try
        {
            if (File.Exists(FilePath))
                store.Settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch
        {
            store.Settings = new AppSettings();
        }
        return store;
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(Settings, JsonOptions));
    }

    public static string ProtectSecret(string plaintext)
    {
        var bytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plaintext), null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    public static string UnprotectSecret(string protectedB64)
    {
        var bytes = ProtectedData.Unprotect(
            Convert.FromBase64String(protectedB64), null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
