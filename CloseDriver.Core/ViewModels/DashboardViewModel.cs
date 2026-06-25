using System;
using System.Threading;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Protocol;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CloseDriver.Core.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
	private readonly IBluetoothService _bluetoothService;
	private readonly FardriverFrameReassembler _reassembler;
	private readonly SynchronizationContext _syncContext;

	[ObservableProperty]
	private FardriverData _currentData;

	[ObservableProperty]
	private bool _hasData;

	[ObservableProperty]
	private int _framesAccepted;

	[ObservableProperty]
	private int _framesRejectedBadCrc;

	[ObservableProperty]
	private int _framesRejectedMalformed;

	public DashboardViewModel(IBluetoothService bluetoothService, FardriverFrameReassembler reassembler)
	{
		_bluetoothService = bluetoothService;
		_reassembler = reassembler;
		_syncContext = SynchronizationContext.Current;
		_bluetoothService.DataReceived += OnDataReceived;
	}

	private void OnDataReceived(object sender, byte[] data)
	{
		if (data == null || data.Length != 16)
			return;

		if (!_reassembler.TryIngest(data, out _))
		{
			UpdateDiagnostics();
			return;
		}

		var snapshot = _reassembler.Snapshot();

		if (_syncContext != null)
		{
			_syncContext.Send(_ =>
			{
				CurrentData = snapshot;
				HasData = true;
				UpdateDiagnostics();
			}, null);
		}
		else
		{
			CurrentData = snapshot;
			HasData = true;
			UpdateDiagnostics();
		}
	}

	private void UpdateDiagnostics()
	{
		FramesAccepted = _reassembler.FramesAccepted;
		FramesRejectedBadCrc = _reassembler.FramesRejectedBadCrc;
		FramesRejectedMalformed = _reassembler.FramesRejectedMalformed;
	}

	public void Dispose()
	{
		_bluetoothService.DataReceived -= OnDataReceived;
	}
}
