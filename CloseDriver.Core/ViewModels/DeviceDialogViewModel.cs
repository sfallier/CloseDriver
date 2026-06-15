using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CloseDriver.Core.ViewModels;

public partial class DeviceDialogViewModel : ObservableObject
{
	private readonly IBluetoothService _bluetoothService;

	[ObservableProperty]
	private ObservableCollection<BluetoothDevice> _discoveredDevices = new();

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SelectDeviceCommand))]
	private BluetoothDevice? _selectedDevice;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private bool _isStatusError;

	public BluetoothDevice? ConfirmedDevice { get; private set; }

	public DeviceDialogViewModel(IBluetoothService bluetoothService)
	{
		_bluetoothService = bluetoothService;
		_bluetoothService.DeviceDiscovered += OnDeviceDiscovered;
		_bluetoothService.ScanStatusChanged += OnScanStatusChanged;
	}

	private void OnScanStatusChanged(object? sender, ScanStatus status)
	{
		StatusMessage = status.Message;
		IsStatusError = status.IsError;
	}

	private void OnDeviceDiscovered(object? sender, BluetoothDevice device)
	{
		// Must be marshalled to the UI thread in WPF, but ObservableCollection in Core
		// will rely on WPF binding dispatcher or we handle it in the View.
		// For simplicity, assuming the caller marshals or we use BindingOperations.EnableCollectionSynchronization

		var existingDevice = DiscoveredDevices.FirstOrDefault(d => d.Id == device.Id);
		if (existingDevice == null)
		{
			DiscoveredDevices.Add(device);
		}
		else
		{
			// Update RSSI
			existingDevice.Rssi = device.Rssi;
		}
	}

	[RelayCommand]
	public async Task StartScanningAsync()
	{
		DiscoveredDevices.Clear();
		await _bluetoothService.StartScanningAsync();
	}

	[RelayCommand]
	public async Task StopScanningAsync()
	{
		await _bluetoothService.StopScanningAsync();
	}

	[RelayCommand(CanExecute = nameof(CanSelectDevice))]
	private void SelectDevice()
	{
		ConfirmedDevice = SelectedDevice;
	}

	private bool CanSelectDevice()
	{
		return SelectedDevice != null;
	}

	public async Task CleanupAsync()
	{
		_bluetoothService.DeviceDiscovered -= OnDeviceDiscovered;
		_bluetoothService.ScanStatusChanged -= OnScanStatusChanged;
		await _bluetoothService.StopScanningAsync();
	}
}
