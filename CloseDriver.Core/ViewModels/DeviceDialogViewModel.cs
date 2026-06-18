using System;
using System.Collections.ObjectModel;
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
	private BluetoothDevice _selectedDevice;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private bool _isStatusError;

	[ObservableProperty]
	private BleScanMode _scanMode = BleScanMode.ControllersOnly;

	public BluetoothDevice ConfirmedDevice { get; private set; }

	public DeviceDialogViewModel(IBluetoothService bluetoothService)
	{
		_bluetoothService = bluetoothService;
		_bluetoothService.DeviceDiscovered += OnDeviceDiscovered;
		_bluetoothService.ScanStatusChanged += OnScanStatusChanged;
	}

	private void OnScanStatusChanged(object sender, ScanStatus status)
	{
		StatusMessage = status.Message;
		IsStatusError = status.IsError;
	}

	private void OnDeviceDiscovered(object sender, BluetoothDevice device)
	{
		BluetoothDevice existingDevice = null;
		for (int i = 0; i < DiscoveredDevices.Count; i++)
		{
			if (DiscoveredDevices[i].Id == device.Id) { existingDevice = DiscoveredDevices[i]; break; }
		}
		if (existingDevice == null)
		{
			InsertSorted(device);
		}
		else
		{
			existingDevice.Rssi = device.Rssi;

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
		bool aIsUnnamed = a.StartsWith(BluetoothDevice.kUnnamedPrefix, StringComparison.Ordinal);
		bool bIsUnnamed = b.StartsWith(BluetoothDevice.kUnnamedPrefix, StringComparison.Ordinal);

		if (aIsUnnamed != bIsUnnamed)
			return aIsUnnamed ? 1 : -1;

		return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
	}

	[RelayCommand]
	public async Task StartScanningAsync()
	{
		DiscoveredDevices.Clear();
		await _bluetoothService.StartScanningAsync(ScanMode);
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
