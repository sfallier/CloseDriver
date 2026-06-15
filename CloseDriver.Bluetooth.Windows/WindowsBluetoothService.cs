using CloseDriver.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Radios;
using Windows.Storage.Streams;
using CloseDriver.Core.Models;
using CoreBluetoothDevice = CloseDriver.Core.Models.BluetoothDevice;

namespace CloseDriver.Bluetooth.Windows;

public class WindowsBluetoothService : IBluetoothService
{
	private readonly ILogger<WindowsBluetoothService>? _logger;
	private BluetoothLEAdvertisementWatcher? _watcher;
	private readonly Dictionary<ulong, CoreBluetoothDevice> _discoveredDevices = new();
	private int _advertisementCount;
	private BluetoothLEDevice? _connectedDevice;
	private GattCharacteristic? _txCharacteristic;
	private GattCharacteristic? _rxCharacteristic;

	// Standard BLE Serial Service UUIDs, these might need to be adjusted for FarDriver specifically
	// commonly it's something like 0xFFE0 / 0xFFE1 but we will try generic ones or let it be discovered
	private static readonly Guid FarDriverServiceUuid = Guid.Parse("0000ffe0-0000-1000-8000-00805f9b34fb"); // Example standard TI CC254x SPP service
	private static readonly Guid FarDriverRxCharacteristicUuid = Guid.Parse("0000ffe1-0000-1000-8000-00805f9b34fb");
	private static readonly Guid FarDriverTxCharacteristicUuid = Guid.Parse("0000ffe1-0000-1000-8000-00805f9b34fb");

	public event EventHandler<CoreBluetoothDevice>? DeviceDiscovered;
	public event EventHandler<byte[]>? DataReceived;
	public event EventHandler<ScanStatus>? ScanStatusChanged;

	public bool IsScanning { get; private set; }

	public WindowsBluetoothService(ILogger<WindowsBluetoothService>? logger = null)
	{
		_logger = logger;
	}

	public async Task StartScanningAsync()
	{
		if (IsScanning)
			return;

		_discoveredDevices.Clear();
		_advertisementCount = 0;

		var radioCheck = await CheckRadioAsync();
		if (radioCheck != null)
		{
			RaiseStatus(radioCheck);
			return;
		}

		_watcher = new BluetoothLEAdvertisementWatcher
		{
			// Active scanning sends SCAN_REQ packets so peripherals reply with their
			// scan response (which usually carries the LocalName and full service UUID
			// list). Without this most household BLE devices appear nameless.
			ScanningMode = BluetoothLEScanningMode.Active,
		};

		_watcher.Received += OnAdvertisementReceived;
		_watcher.Stopped += OnWatcherStopped;

		try
		{
			_watcher.Start();
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Failed to start BLE advertisement watcher.");
			RaiseStatus(new ScanStatus(false, $"Failed to start scan: {ex.Message}", true));
			return;
		}

		IsScanning = true;
		_logger?.LogInformation("BLE advertisement watcher started (ScanningMode=Active, Status={Status}).", _watcher.Status);
		RaiseStatus(new ScanStatus(true, "Scanning for devices...", false));
	}

	private async Task<ScanStatus?> CheckRadioAsync()
	{
		try
		{
			var radios = await Radio.GetRadiosAsync();
			var bt = radios.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);
			if (bt == null)
			{
				_logger?.LogWarning("No Bluetooth radio found on this machine.");
				return new ScanStatus(false, "No Bluetooth radio was found on this machine.", true);
			}
			if (bt.State != RadioState.On)
			{
				_logger?.LogWarning("Bluetooth radio state is {State}; scan will return no results until it is On.", bt.State);
				return new ScanStatus(false, $"Bluetooth radio is {bt.State}. Turn Bluetooth on and try again.", true);
			}
			_logger?.LogInformation("Bluetooth radio '{Name}' is On.", bt.Name);
			return null;
		}
		catch (Exception ex)
		{
			_logger?.LogWarning(ex, "Failed to query Bluetooth radio state.");
			return new ScanStatus(false, $"Could not query Bluetooth radio: {ex.Message}", true);
		}
	}

	private void RaiseStatus(ScanStatus status)
	{
		ScanStatusChanged?.Invoke(this, status);
	}

	private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
	{
		IsScanning = false;
		if (args.Error == BluetoothError.Success)
		{
			_logger?.LogInformation("BLE advertisement watcher stopped normally. Ads seen: {Count}.", _advertisementCount);
		}
		else
		{
			_logger?.LogError("BLE advertisement watcher stopped with error: {Error}. Ads seen: {Count}.", args.Error, _advertisementCount);
			RaiseStatus(new ScanStatus(false, $"Scan stopped: {args.Error}", true));
		}
	}

	private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
	{
		_advertisementCount++;

		// LocalName arrives in different ad sections depending on the advertiser; Active scanning
		// also delivers ScanResponse packets where the name typically lives. Fall back to MAC if missing.
		var deviceName = args.Advertisement.LocalName;
		var macFormatted = FormatMac(args.BluetoothAddress);

		if (!_discoveredDevices.TryGetValue(args.BluetoothAddress, out var device))
		{
			device = new CoreBluetoothDevice
			{
				Id = args.BluetoothAddress.ToString(),
				Name = string.IsNullOrWhiteSpace(deviceName) ? $"(unnamed) {macFormatted}" : deviceName,
				Rssi = args.RawSignalStrengthInDBm,
				NativeDevice = args.BluetoothAddress,
			};
			_discoveredDevices[args.BluetoothAddress] = device;
			_logger?.LogDebug("BLE device discovered: {Name} [{Mac}] RSSI={Rssi} AdType={AdType}", device.Name, macFormatted, device.Rssi, args.AdvertisementType);
			DeviceDiscovered?.Invoke(this, device);
		}
		else
		{
			// Upgrade name when a scan-response with the LocalName arrives later.
			if (!string.IsNullOrWhiteSpace(deviceName) && device.Name.StartsWith("(unnamed)", StringComparison.Ordinal))
			{
				device.Name = deviceName;
				_logger?.LogDebug("BLE device named via scan-response: {Name} [{Mac}]", deviceName, macFormatted);
				DeviceDiscovered?.Invoke(this, device);
			}
			device.Rssi = args.RawSignalStrengthInDBm;
		}
	}

	private static string FormatMac(ulong address)
	{
		return string.Join(":", BitConverter.GetBytes(address).Take(6).Reverse().Select(b => b.ToString("X2")));
	}

	public Task StopScanningAsync()
	{
		if (!IsScanning || _watcher == null)
			return Task.CompletedTask;

		_watcher.Stop();
		_watcher.Received -= OnAdvertisementReceived;
		_watcher.Stopped -= OnWatcherStopped;

		IsScanning = false;

		return Task.CompletedTask;
	}



	public async Task<bool> ConnectAsync(CoreBluetoothDevice device)
	{
		if (device.NativeDevice is not ulong bluetoothAddress)
			return false;

		try
		{
			_connectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
			if (_connectedDevice == null)
				return false;

			_connectedDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

			var servicesResult = await _connectedDevice.GetGattServicesAsync();
			if (servicesResult.Status != GattCommunicationStatus.Success)
				return false;

			foreach (var service in servicesResult.Services)
			{
				// We could filter by FarDriverServiceUuid, but for now let's just grab the first one that works or matching ones
				var charsResult = await service.GetCharacteristicsAsync();
				if (charsResult.Status == GattCommunicationStatus.Success)
				{
					foreach (var c in charsResult.Characteristics)
					{
						if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
						{
							_rxCharacteristic = c;
							var status = await c.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
							if (status == GattCommunicationStatus.Success)
							{
								c.ValueChanged += OnCharacteristicValueChanged;
							}
						}
						if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) ||
							c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
						{
							_txCharacteristic = c;
						}
					}
				}
			}

			return _rxCharacteristic != null; // Connected successfully if we can at least listen
		}
		catch (Exception)
		{
			return false;
		}
	}

	private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
	{
		if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
		{
			_ = DisconnectAsync();
		}
	}

	private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
	{
		var reader = DataReader.FromBuffer(args.CharacteristicValue);
		var bytes = new byte[reader.UnconsumedBufferLength];
		reader.ReadBytes(bytes);

		DataReceived?.Invoke(this, bytes);
	}

	public Task DisconnectAsync()
	{
		if (_rxCharacteristic != null)
		{
			_rxCharacteristic.ValueChanged -= OnCharacteristicValueChanged;
			_rxCharacteristic = null;
		}

		_txCharacteristic = null;

		if (_connectedDevice != null)
		{
			_connectedDevice.ConnectionStatusChanged -= OnConnectionStatusChanged;
			_connectedDevice.Dispose();
			_connectedDevice = null;
		}

		return Task.CompletedTask;
	}

	public async Task<bool> WriteDataAsync(byte[] data)
	{
		if (_txCharacteristic == null)
			return false;

		var writer = new DataWriter();
		writer.WriteBytes(data);

		var result = await _txCharacteristic.WriteValueAsync(writer.DetachBuffer());
		return result == GattCommunicationStatus.Success;
	}
}
