using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
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

    public Task<bool> ConnectAsync(CoreBluetoothDevice device)
    {
        // Implementation for connecting to characteristics will go here later
        throw new NotImplementedException();
    }

    public Task DisconnectAsync()
    {
        // Implementation for disconnecting
        throw new NotImplementedException();
    }

    public Task<bool> WriteDataAsync(byte[] data)
    {
        // Implementation for writing to characteristics
        throw new NotImplementedException();
    }
}
