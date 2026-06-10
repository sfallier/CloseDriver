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

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
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
