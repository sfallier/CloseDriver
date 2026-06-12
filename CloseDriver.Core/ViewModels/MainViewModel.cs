using System;
using System.IO;
using System.Threading.Tasks;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Models;
using CloseDriver.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CloseDriver.Core.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
	private readonly IBluetoothService _bluetoothService;
	private DataLogger? _dataLogger;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
	[NotifyCanExecuteChangedFor(nameof(ToggleLoggingCommand))]
	private BluetoothDevice? _connectedDevice;

	[ObservableProperty]
	private bool _isLogging;

	public MainViewModel(IBluetoothService bluetoothService)
	{
		_bluetoothService = bluetoothService;
		_bluetoothService.DataReceived += OnDataReceived;
	}

	private async void OnDataReceived(object? sender, byte[] data)
	{
		if (IsLogging && _dataLogger != null)
		{
			await _dataLogger.LogDataAsync(data, isTransmit: false);
		}
	}

	[RelayCommand]
	private async Task ScanForDevicesAsync()
	{
		// This is a placeholder since the actual dialog logic is in WPF
		await Task.CompletedTask;
	}

	public async Task ConnectDeviceAsync(BluetoothDevice device)
	{
		var success = await _bluetoothService.ConnectAsync(device);
		if (success)
		{
			ConnectedDevice = device;
		}
	}

	[RelayCommand(CanExecute = nameof(CanDisconnect))]
	private async Task DisconnectAsync()
	{
		await _bluetoothService.DisconnectAsync();
		ConnectedDevice = null;

		if (IsLogging)
		{
			ToggleLogging();
		}
	}

	private bool CanDisconnect() => ConnectedDevice != null;

	[RelayCommand(CanExecute = nameof(CanToggleLogging))]
	private void ToggleLogging()
	{
		if (IsLogging)
		{
			_dataLogger?.Stop();
			IsLogging = false;
		}
		else
		{
			var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var filePath = Path.Combine(folderPath, "CloseDriver", $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

			_dataLogger = new DataLogger(filePath);
			_dataLogger.Start();
			IsLogging = true;
		}
	}

	private bool CanToggleLogging() => ConnectedDevice != null;

	public void Dispose()
	{
		_bluetoothService.DataReceived -= OnDataReceived;
		_dataLogger?.Dispose();
	}
}
