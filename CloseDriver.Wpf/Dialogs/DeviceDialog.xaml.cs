using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CloseDriver.Core.ViewModels;

namespace CloseDriver.Wpf.Dialogs;

public partial class DeviceDialog : Window
{
    private readonly DeviceDialogViewModel _viewModel;

    public DeviceDialog(DeviceDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Enable Cross-thread updates for the ObservableCollection
        BindingOperations.EnableCollectionSynchronization(_viewModel.DiscoveredDevices, new object());
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.StartScanningAsync();
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        await _viewModel.CleanupAsync();
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.ConfirmedDevice != null)
        {
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
