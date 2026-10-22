using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PlateWatch.App.Services;

namespace PlateWatch.App;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsStore _store;

    public SettingsWindow(SettingsStore store)
    {
        _store = store;
        InitializeComponent();
        Title = "PlateWatch Settings";
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 720));

        ToastsToggle.IsOn = _store.Settings.ToastsEnabled;
        PrintStartToggle.IsOn = _store.Settings.ToastOnPrintStart;
        PrintStartToggle.IsEnabled = _store.Settings.ToastsEnabled;
        StartupToggle.IsOn = _store.Settings.StartWithWindows;
        RefreshPrinterList();
    }

    private void RefreshPrinterList() => PrinterList.ItemsSource = _store.Settings.Printers;

    private void OnToastsToggled(object sender, RoutedEventArgs e)
    {
        _store.Settings.ToastsEnabled = ToastsToggle.IsOn;
        PrintStartToggle.IsEnabled = ToastsToggle.IsOn;
        _store.Save();
    }

    private void OnPrintStartToggled(object sender, RoutedEventArgs e)
    {
        _store.Settings.ToastOnPrintStart = PrintStartToggle.IsOn;
        _store.Save();
    }

    private void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        _store.Settings.StartWithWindows = StartupToggle.IsOn;
        StartupService.SetEnabled(StartupToggle.IsOn);
        _store.Save();
    }

    private void OnAddPrinter(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(IpBox.Text)
            || string.IsNullOrWhiteSpace(SerialBox.Text))
        {
            AddError.Text = "Name, IP address and serial are required.";
            AddError.Visibility = Visibility.Visible;
            return;
        }
        AddError.Visibility = Visibility.Collapsed;
        _store.Settings.Printers.Add(new PrinterConfig
        {
            Name = NameBox.Text.Trim(),
            Model = ModelBox.Text.Trim(),
            IpAddress = IpBox.Text.Trim(),
            Serial = SerialBox.Text.Trim(),
            AccessCodeProtected = string.IsNullOrEmpty(AccessCodeBox.Password)
                ? ""
                : SettingsStore.ProtectSecret(AccessCodeBox.Password),
        });
        _store.Save();
        NameBox.Text = ModelBox.Text = IpBox.Text = SerialBox.Text = AccessCodeBox.Password = "";
        RefreshPrinterList();
    }
}
