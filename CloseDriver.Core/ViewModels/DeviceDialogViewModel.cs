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
		var existingDevice = DiscoveredDevices.FirstOrDefault(d => d.Id == device.Id);
		if (existingDevice == null)
		{
			InsertSorted(device);
		}
		else
		{
			existingDevice.Rssi = device.Rssi;

			// If the name was upgraded from "(unnamed)" to a real name, re-insert at the
			// correct sorted position.
			if (device.Name != existingDevice.Name)
			{
				existingDevice.Name = device.Name;
				var idx = DiscoveredDevices.IndexOf(existingDevice);
				DiscoveredDevices.RemoveAt(idx);
				InsertSorted(existingDevice);
			}
		}
	}

	private void InsertSorted(BluetoothDevice device)
	{
		for (int i = 0; i < DiscoveredDevices.Count; i++)
		{
			if (CompareDeviceNames(device.Name, DiscoveredDevices[i].Name) < 0)
			{
				DiscoveredDevices.Insert(i, device);
				return;
			}
		}
		DiscoveredDevices.Add(device);
	}

	private static int CompareDeviceNames(string a, string b)
	{
		bool aUnnamed = a.StartsWith("(unnamed)", StringComparison.Ordinal);
		bool bUnnamed = b.StartsWith("(unnamed)", StringComparison.Ordinal);

		if (aUnnamed != bUnnamed)
			return aUnnamed ? 1 : -1;

		return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
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
