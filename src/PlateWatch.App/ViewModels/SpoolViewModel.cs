using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using PlateWatch.Core;

namespace PlateWatch.App.ViewModels;

public partial class SpoolViewModel : ObservableObject
{
    public SpoolViewModel(Spool spool)
    {
        Slot = spool.Slot;
        Material = spool.Material;
        Remaining01 = spool.Remaining01;
        IsActive = spool.Active;
        IsLow = spool.IsLow;
        Color = Parse(spool.ColorHex);
    }

    public int Slot { get; }
    public string Material { get; }
    public double Remaining01 { get; }
    public bool IsActive { get; }
    public bool IsLow { get; }
    public Color Color { get; }
    public Brush ColorBrush => new SolidColorBrush(Color);
    public double RemainingPct => Remaining01 * 100;
    public string RemainingText => $"{RemainingPct:0}%";

    public static Color Parse(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(255,
            System.Convert.ToByte(hex[..2], 16),
            System.Convert.ToByte(hex.Substring(2, 2), 16),
            System.Convert.ToByte(hex.Substring(4, 2), 16));
    }
}
