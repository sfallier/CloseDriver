using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Models;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Radios;
using Windows.Storage.Streams;
using CoreBluetoothDevice = CloseDriver.Core.Models.BluetoothDevice;

namespace CloseDriver.Bluetooth.Windows;

public class WindowsBluetoothService : IBluetoothService
{
	private readonly ILogger<WindowsBluetoothService> _logger;
	private BluetoothLEAdvertisementWatcher _watcher;
	private readonly Dictionary<ulong, CoreBluetoothDevice> _discoveredDevices = new();
	private int _advertisementCount;
	private BleScanMode _currentScanMode;
	private BluetoothLEDevice _connectedDevice;
	private GattCharacteristic _txCharacteristic;
	private GattCharacteristic _rxCharacteristic;

	// HM-10 BLE UART service / characteristic UUIDs.
	// Service 0xFFE0 is the standard HM-10 serial service UUID, used by FarDriver (CONTROLDM*) and ANT BMS (ANT@BLE*).
	// Characteristic 0xFFEC is the HM-10 TX characteristic (AT+TUUID=FFEC per jackhumbert/fardriver-controllers).
	// Both Rx and Tx share the same characteristic on this profile.
	private static readonly Guid sFarDriverServiceUuid = Guid.Parse("0000ffe0-0000-1000-8000-00805f9b34fb");
	private static readonly Guid sFarDriverRxCharacteristicUuid = Guid.Parse("0000ffec-0000-1000-8000-00805f9b34fb");
	private static readonly Guid sFarDriverTxCharacteristicUuid = Guid.Parse("0000ffec-0000-1000-8000-00805f9b34fb");

	// BT SIG Assigned Numbers, Section 7 — https://www.bluetooth.com/specifications/assigned-numbers/
	// Covers the vendors most commonly seen in BLE scans; expand from the link above as needed.
	private static readonly Dictionary<ushort, string> sCompanyIds = new()
	{
		{ 0x0000, "Ericsson AB" },
		{ 0x0001, "Nokia Technologies Oy" },
		{ 0x0002, "Intel Corp." },
		{ 0x0003, "IBM Corp." },
		{ 0x0004, "Toshiba Corp." },
		{ 0x0006, "Microsoft Corp." },
		{ 0x000D, "Texas Instruments Inc." },  // HM-10 / CC2540 BLE UART modules
		{ 0x0046, "Broadcom Corp." },
		{ 0x004C, "Apple Inc." },
		{ 0x0056, "Sony Corp." },
		{ 0x0059, "Nordic Semiconductor ASA" },
		{ 0x0075, "Samsung Electronics Co. Ltd." },
		{ 0x00E0, "Google LLC" },
		{ 0x0157, "Qualcomm Technologies Inc." },
		{ 0x0171, "Silicon Laboratories Inc." },
		{ 0x01D7, "Cypress Semiconductor Corp." },
		{ 0x01FF, "Microchip Technology Inc." },
		{ 0x02AB, "Realtek Semiconductor Corp." },
		{ 0x02D5, "Espressif Inc." },           // ESP32 devices
		{ 0x02E5, "Anhui Huami Info. Technology Co. Ltd." }, // Xiaomi / Mi Band
		{ 0x0499, "Ruuvi Innovations Ltd." },
	};

	public event EventHandler<CoreBluetoothDevice> DeviceDiscovered;
	public event EventHandler<byte[]> DataReceived;
	public event EventHandler<ScanStatus> ScanStatusChanged;

	public bool IsScanning { get; private set; }

	public WindowsBluetoothService(ILogger<WindowsBluetoothService> logger = null)
	{
		_logger = logger;
	}

	public async Task StartScanningAsync(BleScanMode mode = BleScanMode.ControllersOnly)
	{
		if (IsScanning)
		{
			// Tear down the current watcher without raising a status event so that
			// a mode-change restart is transparent to the ViewModel.
			_watcher.Received -= OnAdvertisementReceived;
			_watcher.Stopped -= OnWatcherStopped;
			_watcher.Stop();
			_watcher = null;
			IsScanning = false;
			_logger?.LogDebug("BLE watcher restarting with new mode {Mode}.", mode);
		}

		_currentScanMode = mode;
		_discoveredDevices.Clear();
		_advertisementCount = 0;

		var radioCheck = await CheckRadioAsync();
		if (radioCheck != null)
		{
			RaiseStatus(radioCheck);
			return;
		}

		bool passiveMode = mode == BleScanMode.AllDevicesPassive;
		_watcher = new BluetoothLEAdvertisementWatcher
		{
			// Active scanning sends SCAN_REQ so peripherals reply with their scan response
			// (which usually carries LocalName and full service UUID list).
			// Passive mode skips SCAN_REQ — used as a diagnostic when testing directed-advertising
			// behaviour (HM-10 may not respond to scan requests from an unbound PC).
			ScanningMode = passiveMode
				? BluetoothLEScanningMode.Passive
				: BluetoothLEScanningMode.Active,
		};

		// Apply OS-level service UUID filter for modes that only care about 0xFFE0 devices.
		// This eliminates Apple, Samsung, and other non-HM10 devices before they reach our callback.
		if (mode == BleScanMode.ControllersOnly || mode == BleScanMode.AllHM10)
		{
			_watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(sFarDriverServiceUuid);
		}

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
		_logger?.LogInformation(
			"BLE advertisement watcher started (Mode={Mode} ScanningMode={ScanningMode} Status={Status}).",
			mode, _watcher.ScanningMode, _watcher.Status);
		RaiseStatus(new ScanStatus(true, "Scanning for devices...", false));
	}

	private async Task<ScanStatus> CheckRadioAsync()
	{
		try
		{
			var radios = await Radio.GetRadiosAsync();
			Radio bt = null;
			foreach (var radio in radios)
			{
				if (radio.Kind == RadioKind.Bluetooth) 
				{ 
					bt = radio; 
					break; 
				}
			}
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
			_logger?.LogInformation("Bluetooth radio '{Name}' (Kind={Kind}) is On.", bt.Name, bt.Kind);
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

	private string GetNameFromAdvert(BluetoothLEAdvertisement advert)
	{
		if (!string.IsNullOrWhiteSpace(advert.LocalName))
			return advert.LocalName;

		TryGetManufacturerName(advert, out string mfr);
		TryGetAppearanceHint(advert, out string appearance);

		if (mfr != null && appearance != null) 
			return $"{mfr} {appearance}";

		if (mfr != null) 
			return mfr;

		if (appearance != null) 
			return appearance;

		return null;
	}

	private static bool TryGetManufacturerName(BluetoothLEAdvertisement advert, out string name)
	{
		if (advert.ManufacturerData.Count > 0)
		{
			var companyId = advert.ManufacturerData[0].CompanyId;
			if (sCompanyIds.TryGetValue(companyId, out name))
				return true;
		}
		name = null;
		return false;
	}

	private static bool TryGetAppearanceHint(BluetoothLEAdvertisement advert, out string hint)
	{
		foreach (var section in advert.DataSections)
		{
			if (section.DataType != BluetoothLEAdvertisementDataTypes.Appearance)
				continue;
			if (section.Data.Length < 2)
				break;

			var reader = DataReader.FromBuffer(section.Data);
			reader.ByteOrder = ByteOrder.LittleEndian;
			ushort raw = reader.ReadUInt16();
			ushort category = (ushort)(raw >> 6);
			hint = AppearanceCategoryToString(category);
			return hint != null;
		}
		hint = null;
		return false;
	}

	private static string AppearanceCategoryToString(ushort category)
	{
		switch (category)
		{
			case  1: return "[Phone]";
			case  2: return "[Computer]";
			case  3: return "[Watch]";
			case  4: return "[Clock]";
			case  5: return "[Display]";
			case  6: return "[Remote Control]";
			case  8: return "[Tag]";
			case  9: return "[Keyring]";
			case 10: return "[Media Player]";
			case 11: return "[Barcode Scanner]";
			case 12: return "[Thermometer]";
			case 13: return "[Heart Rate]";
			case 14: return "[Blood Pressure]";
			case 15: return "[HID]";
			case 16: return "[Glucose Meter]";
			case 17: return "[Running/Walking Sensor]";
			case 18: return "[Cycling]";
			case 49: return "[Pulse Oximeter]";
			case 50: return "[Weight Scale]";
			default: return null;
		}
	}

	private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
	{
		_advertisementCount++;

		// Apple devices are never relevant — skip immediately to reduce noise.
		// This applies in all scan modes; Apple is excluded before it reaches the ViewModel.
		for (int i = 0; i < args.Advertisement.ManufacturerData.Count; i++)
		{
			if (args.Advertisement.ManufacturerData[i].CompanyId == 0x004C)
				return;
		}

		// LocalName arrives in different ad sections depending on the advertiser; Active scanning
		// also delivers ScanResponse packets where the name typically lives. Fall back to MAC if missing.
		string deviceName = GetNameFromAdvert(args.Advertisement);
		string macFormatted = FormatMac(args.BluetoothAddress);

		if (!_discoveredDevices.TryGetValue(args.BluetoothAddress, out var device))
		{
			device = new CoreBluetoothDevice
			{
				Id = args.BluetoothAddress.ToString(),
				Name = string.IsNullOrWhiteSpace(deviceName) ? $"{CoreBluetoothDevice.kUnnamedPrefix} {macFormatted}" : deviceName,
				Rssi = args.RawSignalStrengthInDBm,
				NativeDevice = args.BluetoothAddress,
				Kind = ClassifyDevice(deviceName, args.Advertisement),
			};
			_discoveredDevices[args.BluetoothAddress] = device;
			LogAdvertDump(args, macFormatted);
			_logger?.LogDebug("BLE device discovered: {Name} [{Mac}] RSSI={Rssi} AdType={AdType} Kind={Kind}", device.Name, macFormatted, device.Rssi, args.AdvertisementType, device.Kind);
			if (ShouldRaiseDevice(device))
				DeviceDiscovered?.Invoke(this, device);
		}
		else
		{
			// If the primary advertisement arrives after a scan-response was the first event,
			// the initial dump had empty data sections. Log a follow-up dump when a primary ad
			// arrives carrying service UUIDs or manufacturer data.
			if (args.AdvertisementType != BluetoothLEAdvertisementType.ScanResponse &&
				(args.Advertisement.ServiceUuids.Count > 0 || args.Advertisement.ManufacturerData.Count > 0))
			{
				LogAdvertDump(args, macFormatted);
				// Re-classify now that we have service UUID data.
				device.Kind = ClassifyDevice(device.Name, args.Advertisement);
			}

			// Upgrade name when a scan-response with the LocalName arrives later.
			if (!string.IsNullOrWhiteSpace(deviceName) && device.Name.StartsWith(CoreBluetoothDevice.kUnnamedPrefix, StringComparison.Ordinal))
			{
				device.Name = deviceName;
				device.Kind = ClassifyDevice(deviceName, args.Advertisement);
				_logger?.LogDebug("BLE device named via scan-response: {Name} [{Mac}] Kind={Kind}", deviceName, macFormatted, device.Kind);
				if (ShouldRaiseDevice(device))
					DeviceDiscovered?.Invoke(this, device);
			}
			device.Rssi = args.RawSignalStrengthInDBm;
		}
	}

	private bool ShouldRaiseDevice(CoreBluetoothDevice device)
	{
		if (_currentScanMode == BleScanMode.ControllersOnly)
			return device.Kind == DeviceKind.FarDriverController;
		return true;
	}

	private static DeviceKind ClassifyDevice(string name, BluetoothLEAdvertisement advertisement)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			if (name.IndexOf("CONTROLDM", StringComparison.OrdinalIgnoreCase) >= 0)
				return DeviceKind.FarDriverController;
			if (name.StartsWith("ANT@BLE", StringComparison.OrdinalIgnoreCase))
				return DeviceKind.AntBms;
		}

		for (int i = 0; i < advertisement.ServiceUuids.Count; i++)
		{
			if (advertisement.ServiceUuids[i] == sFarDriverServiceUuid)
				return DeviceKind.OtherHM10;
		}

		return DeviceKind.Unknown;
	}

	private void LogAdvertDump(BluetoothLEAdvertisementReceivedEventArgs args, string mac)
	{
		if (_logger == null || !_logger.IsEnabled(LogLevel.Debug))
			return;

		// Build manufacturer data string: "0x1388:[AA BB CC ...]" for each entry.
		var mfgSb = new System.Text.StringBuilder();
		for (int i = 0; i < args.Advertisement.ManufacturerData.Count; i++)
		{
			if (i > 0) mfgSb.Append(',');
			var mfg = args.Advertisement.ManufacturerData[i];
			mfgSb.Append($"0x{mfg.CompanyId:X4}");
			if (mfg.Data.Length > 0)
			{
				mfgSb.Append(":[");
				var reader = DataReader.FromBuffer(mfg.Data);
				var bytes = new byte[mfg.Data.Length];
				reader.ReadBytes(bytes);
				for (int b = 0; b < bytes.Length; b++)
				{
					if (b > 0) mfgSb.Append(' ');
					mfgSb.Append($"{bytes[b]:X2}");
				}
				mfgSb.Append(']');
			}
		}

		var svcSb = new System.Text.StringBuilder();
		for (int i = 0; i < args.Advertisement.ServiceUuids.Count; i++)
		{
			if (i > 0) svcSb.Append(',');
			svcSb.Append(args.Advertisement.ServiceUuids[i].ToString("B"));
		}

		// Build section types string; for 0x16 (Service Data) also log the raw payload.
		var sectionSb = new System.Text.StringBuilder();
		for (int i = 0; i < args.Advertisement.DataSections.Count; i++)
		{
			if (i > 0) sectionSb.Append(',');
			var section = args.Advertisement.DataSections[i];
			sectionSb.Append($"0x{section.DataType:X2}");
			if (section.DataType == 0x16 && section.Data.Length > 0) // Service Data – 16-bit UUID
			{
				sectionSb.Append(":[");
				var reader = DataReader.FromBuffer(section.Data);
				var bytes = new byte[section.Data.Length];
				reader.ReadBytes(bytes);
				for (int b = 0; b < bytes.Length; b++)
				{
					if (b > 0) sectionSb.Append(' ');
					sectionSb.Append($"{bytes[b]:X2}");
				}
				sectionSb.Append(']');
			}
		}

		_logger.LogDebug(
			"BLE advert dump [{Mac}]: Type={AdvertType} LocalName=\"{LocalName}\" Mfg=[{MfgIds}] Services=[{Services}] Sections=[{AdTypes}]",
			mac, args.AdvertisementType, args.Advertisement.LocalName ?? "", mfgSb, svcSb, sectionSb);
	}

	private static string FormatMac(ulong address)
	{
		var b = BitConverter.GetBytes(address);
		return $"{b[5]:X2}:{b[4]:X2}:{b[3]:X2}:{b[2]:X2}:{b[1]:X2}:{b[0]:X2}";
	}

	public Task StopScanningAsync()
	{
		if (!IsScanning || _watcher == null)
			return Task.CompletedTask;

		_watcher.Received -= OnAdvertisementReceived;
		_watcher.Stopped -= OnWatcherStopped;
		_watcher.Stop();

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
			{
				_logger?.LogWarning("GATT GetGattServicesAsync failed: {Status}", servicesResult.Status);
				return false;
			}

			_logger?.LogDebug("GATT connect [{Name}]: {Count} service(s)", device.Name, servicesResult.Services.Count);

			for (int si = 0; si < servicesResult.Services.Count; si++)
			{
				var service = servicesResult.Services[si];
				_logger?.LogDebug("  Service [{Index}]: {Uuid}", si, service.Uuid);

				var charsResult = await service.GetCharacteristicsAsync();
				if (charsResult.Status != GattCommunicationStatus.Success)
				{
					_logger?.LogDebug("    GetCharacteristicsAsync failed: {Status}", charsResult.Status);
					continue;
				}

				for (int ci = 0; ci < charsResult.Characteristics.Count; ci++)
				{
					var c = charsResult.Characteristics[ci];
					_logger?.LogDebug("    Char [{Index}]: {Uuid}  Props={Props}", ci, c.Uuid, c.CharacteristicProperties);

					if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
					{
						_rxCharacteristic = c;
						var status = await c.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
						if (status == GattCommunicationStatus.Success)
						{
							c.ValueChanged += OnCharacteristicValueChanged;
							_logger?.LogDebug("    → Subscribed for Notify on {Uuid}", c.Uuid);
						}
						else
						{
							_logger?.LogWarning("    → Notify subscription failed: {Status}", status);
						}
					}
					if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) ||
						c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
					{
						_txCharacteristic = c;
						_logger?.LogDebug("    → Selected as TX characteristic: {Uuid}", c.Uuid);
					}
				}
			}

			_logger?.LogInformation(
				"GATT connect complete [{Name}]: RX={RxUuid} TX={TxUuid}",
				device.Name,
				_rxCharacteristic?.Uuid.ToString() ?? "none",
				_txCharacteristic?.Uuid.ToString() ?? "none");

			return _rxCharacteristic != null;
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "ConnectAsync failed for {Name}", device.Name);
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
