using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloseDriver.Core.Models;

namespace CloseDriver.Core.Interfaces;

public interface IBluetoothService
{
	event EventHandler<BluetoothDevice> DeviceDiscovered;
	event EventHandler<byte[]> DataReceived;
	event EventHandler<ScanStatus> ScanStatusChanged;

	bool IsScanning { get; }

	Task StartScanningAsync(BleScanMode mode = BleScanMode.ControllersOnly);
	Task StopScanningAsync();

	Task<bool> ConnectAsync(BluetoothDevice device);
	Task DisconnectAsync();

	Task<bool> WriteDataAsync(byte[] data);
}
