using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PlateWatch.App.Services;

namespace PlateWatch.App;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsStore _store;
    private readonly Action? _applyFleet;
    private PrinterConfig? _editing;

    public SettingsWindow(SettingsStore store, Action? applyFleet = null)
    {
        _store = store;
        _applyFleet = applyFleet;
        InitializeComponent();
        Title = "PlateWatch Settings";
        SystemBackdrop = new MicaBackdrop();
        var appWindow = AppWindow;
        if (IconFile.CurrentPath is not null)
            appWindow.SetIcon(IconFile.CurrentPath);
        appWindow.Resize(new Windows.Graphics.SizeInt32(600, 720));

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

    private void OnSavePrinter(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(IpBox.Text)
            || string.IsNullOrWhiteSpace(SerialBox.Text))
        {
            AddError.Text = "Name, IP address and serial are required.";
            AddError.Visibility = Visibility.Visible;
            return;
        }
        AddError.Visibility = Visibility.Collapsed;
        if (_editing is not null)
        {
            _editing.Name = NameBox.Text.Trim();
            _editing.Model = ModelBox.Text.Trim();
            _editing.IpAddress = IpBox.Text.Trim();
            _editing.Serial = SerialBox.Text.Trim();
            // blank access code on edit keeps the stored one
            if (!string.IsNullOrEmpty(AccessCodeBox.Password))
                _editing.AccessCodeProtected = SettingsStore.ProtectSecret(AccessCodeBox.Password);
        }
        else
        {
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
        }
        _store.Save();
        ResetForm();
        RefreshPrinterList();
        _applyFleet?.Invoke();
    }

    private void OnEditPrinter(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not PrinterConfig printer) return;
        _editing = printer;
        NameBox.Text = printer.Name;
        ModelBox.Text = printer.Model;
        IpBox.Text = printer.IpAddress;
        SerialBox.Text = printer.Serial;
        AccessCodeBox.Password = "";
        FormHeader.Text = "Edit printer (blank access code keeps the saved one)";
        AddButton.Content = "Save changes";
        CancelEditButton.Visibility = Visibility.Visible;
    }

    private void OnDeletePrinter(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not PrinterConfig printer) return;
        if (ReferenceEquals(_editing, printer)) ResetForm();
        _store.Settings.Printers.Remove(printer);
        _store.Save();
        RefreshPrinterList();
        _applyFleet?.Invoke();
    }

    private void OnCancelEdit(object sender, RoutedEventArgs e) => ResetForm();

    private void ResetForm()
    {
        _editing = null;
        NameBox.Text = ModelBox.Text = IpBox.Text = SerialBox.Text = AccessCodeBox.Password = "";
        FormHeader.Text = "Add a printer (LAN mode)";
        AddButton.Content = "Add printer";
        CancelEditButton.Visibility = Visibility.Collapsed;
        AddError.Visibility = Visibility.Collapsed;
    }
}
