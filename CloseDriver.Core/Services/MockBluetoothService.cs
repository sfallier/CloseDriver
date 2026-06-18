using System;
using System.Threading.Tasks;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Models;

namespace CloseDriver.Core.Services;

public class MockBluetoothService : IBluetoothService
{
	public event EventHandler<BluetoothDevice> DeviceDiscovered;
	public event EventHandler<byte[]> DataReceived;
	public event EventHandler<ScanStatus> ScanStatusChanged;

	public bool IsScanning { get; private set; }

	public async Task StartScanningAsync(BleScanMode mode = BleScanMode.ControllersOnly)
	{
		if (IsScanning)
			return;

		IsScanning = true;
		ScanStatusChanged?.Invoke(this, new ScanStatus(true, "Scanning for devices...", false));

		// Simulate finding a few devices
		_ = Task.Run(async () =>
		{
			await Task.Delay(500);
			if (IsScanning)
			{
				DeviceDiscovered?.Invoke(this, new BluetoothDevice { Id = "MOCK-1", Name = "CONTROLDM-Mock1", Rssi = -50, Kind = DeviceKind.FarDriverController });
			}

			await Task.Delay(1000);
			if (IsScanning)
			{
				DeviceDiscovered?.Invoke(this, new BluetoothDevice { Id = "MOCK-2", Name = "CONTROLDM-Mock2", Rssi = -70, Kind = DeviceKind.FarDriverController });
			}
		});

		await Task.CompletedTask;
	}

	public Task StopScanningAsync()
	{
		IsScanning = false;
		return Task.CompletedTask;
	}

	public Task<bool> ConnectAsync(BluetoothDevice device)
	{
		// Simulate successful connection
		return Task.FromResult(true);
	}

	public Task DisconnectAsync()
	{
		return Task.CompletedTask;
	}

	public Task<bool> WriteDataAsync(byte[] data)
	{
		return Task.FromResult(true);
	}
}
