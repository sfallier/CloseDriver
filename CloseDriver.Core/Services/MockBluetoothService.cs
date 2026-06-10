using System;
using System.Threading.Tasks;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Models;

namespace CloseDriver.Core.Services;

public class MockBluetoothService : IBluetoothService
{
    public event EventHandler<BluetoothDevice>? DeviceDiscovered;
    public event EventHandler<byte[]>? DataReceived;

    public bool IsScanning { get; private set; }

    public async Task StartScanningAsync()
    {
        if (IsScanning) return;
        
        IsScanning = true;

        // Simulate finding a few devices
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            if (IsScanning)
            {
                DeviceDiscovered?.Invoke(this, new BluetoothDevice { Id = "MOCK-1", Name = "FarDriver-Mock1", Rssi = -50 });
            }

            await Task.Delay(1000);
            if (IsScanning)
            {
                DeviceDiscovered?.Invoke(this, new BluetoothDevice { Id = "MOCK-2", Name = "FarDriver-Mock2", Rssi = -70 });
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
