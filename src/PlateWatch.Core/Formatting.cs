using System.Globalization;

namespace PlateWatch.Core;

public static class Formatting
{
    /// <summary>"59m" / "1h 01m"</summary>
    public static string FormatTimeLeft(double seconds)
    {
        if (seconds <= 0) return "0m";
        var totalMinutes = (int)Math.Round(seconds / 60.0);
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        return h > 0 ? $"{h}h {m:D2}m" : $"{m}m";
    }

    /// <summary>"Done 10:40" (24h clock).</summary>
    public static string FormatFinish(DateTimeOffset now, double secondsLeft)
        => "Done " + now.AddSeconds(secondsLeft).ToString("H:mm", CultureInfo.InvariantCulture);
}
