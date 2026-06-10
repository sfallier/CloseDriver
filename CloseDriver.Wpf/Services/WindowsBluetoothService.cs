using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using CloseDriver.Core.Interfaces;
using CoreBluetoothDevice = CloseDriver.Core.Models.BluetoothDevice;

namespace CloseDriver.Wpf.Services;

public class WindowsBluetoothService : IBluetoothService
{
    private BluetoothLEAdvertisementWatcher? _watcher;
    private readonly Dictionary<ulong, CoreBluetoothDevice> _discoveredDevices = new();

    public event EventHandler<CoreBluetoothDevice>? DeviceDiscovered;
    public event EventHandler<byte[]>? DataReceived;

    public bool IsScanning { get; private set; }

    public Task StartScanningAsync()
    {
        if (IsScanning) return Task.CompletedTask;

        _discoveredDevices.Clear();
        
        _watcher = new BluetoothLEAdvertisementWatcher();
        _watcher.Received += OnAdvertisementReceived;
        _watcher.Stopped += OnWatcherStopped;
        
        _watcher.Start();
        IsScanning = true;

        return Task.CompletedTask;
    }

    private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    {
        IsScanning = false;
    }

    private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        // Avoid adding the same device repeatedly unless we want to update RSSI
        if (!_discoveredDevices.ContainsKey(args.BluetoothAddress))
        {
            var deviceName = args.Advertisement.LocalName;
            
            // Some FarDriver controllers might have specific name prefixes, but we'll include all with names for now
            if (!string.IsNullOrEmpty(deviceName))
            {
                var device = new CoreBluetoothDevice
                {
                    Id = args.BluetoothAddress.ToString(),
                    Name = deviceName,
                    Rssi = args.RawSignalStrengthInDBm,
                    NativeDevice = args.BluetoothAddress // Store the address to connect later
                };
                
                _discoveredDevices[args.BluetoothAddress] = device;
                DeviceDiscovered?.Invoke(this, device);
            }
        }
    }

    public Task StopScanningAsync()
    {
        if (!IsScanning || _watcher == null) return Task.CompletedTask;

        _watcher.Stop();
        _watcher.Received -= OnAdvertisementReceived;
        _watcher.Stopped -= OnWatcherStopped;
        
        IsScanning = false;
        
        return Task.CompletedTask;
    }

    private BluetoothLEDevice? _connectedDevice;
    private GattCharacteristic? _txCharacteristic;
    private GattCharacteristic? _rxCharacteristic;

    // Standard BLE Serial Service UUIDs, these might need to be adjusted for FarDriver specifically
    // commonly it's something like 0xFFE0 / 0xFFE1 but we will try generic ones or let it be discovered
    private static readonly Guid FarDriverServiceUuid = Guid.Parse("0000ffe0-0000-1000-8000-00805f9b34fb"); // Example standard TI CC254x SPP service
    private static readonly Guid FarDriverRxCharacteristicUuid = Guid.Parse("0000ffe1-0000-1000-8000-00805f9b34fb");
    private static readonly Guid FarDriverTxCharacteristicUuid = Guid.Parse("0000ffe1-0000-1000-8000-00805f9b34fb");

    public async Task<bool> ConnectAsync(CoreBluetoothDevice device)
    {
        if (device.NativeDevice is not ulong bluetoothAddress)
            return false;

        try
        {
            _connectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            if (_connectedDevice == null) return false;

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
        var reader = Windows.Storage.Streams.DataReader.FromBuffer(args.CharacteristicValue);
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
        if (_txCharacteristic == null) return false;

        var writer = new Windows.Storage.Streams.DataWriter();
        writer.WriteBytes(data);
        
        var result = await _txCharacteristic.WriteValueAsync(writer.DetachBuffer());
        return result == GattCommunicationStatus.Success;
    }
}
