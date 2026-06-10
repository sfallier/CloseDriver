using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using CloseDriver.Core.ViewModels;
using CloseDriver.Wpf.Dialogs;

namespace CloseDriver.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SettingsViewModel _settingsViewModel;

    public MainWindow(MainViewModel viewModel, SettingsViewModel settingsViewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settingsViewModel = settingsViewModel;
        DataContext = _viewModel;
        
        // Pass the settings view model to the SettingsView control
        DashboardView.DataContext = _settingsViewModel;
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        var deviceDialog = App.Current.Services.GetRequiredService<DeviceDialog>();
        var result = deviceDialog.ShowDialog();
        
        if (result == true)
        {
            var dialogViewModel = (DeviceDialogViewModel)deviceDialog.DataContext;
            if (dialogViewModel.ConfirmedDevice != null)
            {
                await _viewModel.ConnectDeviceAsync(dialogViewModel.ConfirmedDevice);
            }
        }
    }
}
