using System;

namespace CloseDriver.Core.Models;

public class BluetoothDevice
{

	public const string kUnnamedPrefix = "(unnamed)";

	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public int Rssi { get; set; }
	public DeviceKind Kind { get; set; } = DeviceKind.Unknown;

	// Platform specific device object can be stored here if needed
	public object NativeDevice { get; set; }
}
