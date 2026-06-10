using System;

namespace CloseDriver.Core.Models;

public class BluetoothDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Rssi { get; set; }
    
    // Platform specific device object can be stored here if needed
    public object? NativeDevice { get; set; }
}
