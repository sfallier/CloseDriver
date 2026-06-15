namespace CloseDriver.Core.Models;

/// <summary>
/// Human-readable status for the BLE advertisement scan, suitable for binding
/// to a UI status field. <see cref="IsError"/> distinguishes informational
/// states (e.g. "Scanning for devices...") from blocking failures
/// (e.g. "Bluetooth is off").
/// </summary>
public record ScanStatus(bool IsScanning, string Message, bool IsError);
