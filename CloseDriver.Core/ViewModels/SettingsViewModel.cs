using CommunityToolkit.Mvvm.ComponentModel;
using CloseDriver.Core.Protocol;
using CloseDriver.Core.Interfaces;
using System;
using System.Threading;

namespace CloseDriver.Core.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IBluetoothService _bluetoothService;
    private readonly SynchronizationContext? _syncContext;

    [ObservableProperty]
    private FardriverData _currentData;

    [ObservableProperty]
    private bool _hasData;

    public SettingsViewModel(IBluetoothService bluetoothService)
    {
        _bluetoothService = bluetoothService;
        _syncContext = SynchronizationContext.Current;
        _bluetoothService.DataReceived += OnDataReceived;
    }

    private void OnDataReceived(object? sender, byte[] data)
    {
        if (data != null && data.Length >= 512)
        {
            try
            {
                // In a real scenario we'd need to buffer data until we have a full 512-byte frame
                // and identify the start/end markers. For now, assuming data is exactly the 512 byte struct
                var parsedData = FarDriverProtocolParser.Parse(data);
                
                // Update properties on UI thread via the MVVM toolkit
                if (_syncContext != null)
                {
                    _syncContext.Send(_ => 
                    {
                        CurrentData = parsedData;
                        HasData = true;
                    }, null);
                }
                else
                {
                    CurrentData = parsedData;
                    HasData = true;
                }
            }
            catch (Exception ex)
            {
                // Log or handle parsing errors
                System.Diagnostics.Debug.WriteLine($"Failed to parse frame: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _bluetoothService.DataReceived -= OnDataReceived;
    }
}
