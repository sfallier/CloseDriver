namespace CloseDriver.Core.Models;

public enum BleScanMode
{
    ControllersOnly,    // Active scan, OS-level 0xFFE0 filter, raise only FarDriverController devices
    AllHM10,            // Active scan, OS-level 0xFFE0 filter, raise all matching devices
    AllDevices,         // Active scan, no OS filter, raise all non-Apple devices
    AllDevicesPassive,  // Passive scan, no OS filter — diagnostic for directed-advertising hypothesis
}
