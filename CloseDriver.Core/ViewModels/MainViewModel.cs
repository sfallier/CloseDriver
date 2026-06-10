using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CloseDriver.Core.Interfaces;
using System.Threading.Tasks;

namespace CloseDriver.Core.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBluetoothService _bluetoothService;

    public MainViewModel(IBluetoothService bluetoothService)
    {
        _bluetoothService = bluetoothService;
    }

    [RelayCommand]
    private async Task ScanForDevicesAsync()
    {
        // For now, just trigger scanning. 
        // Later we will open the DeviceDialog and scan from there or handle the UI logic.
        await _bluetoothService.StartScanningAsync();
    }
}
