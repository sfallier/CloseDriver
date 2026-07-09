using System;
using System.Threading;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Protocol;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CloseDriver.Core.ViewModels;

/// <summary>
/// View model for the controller configuration tab. Mirrors incoming BLE frames into
/// a <see cref="FardriverData"/> snapshot and exposes per-section availability flags.
/// All fields are read-only; writes to the controller are out of scope for this pass.
/// </summary>
public partial class ConfigurationViewModel : ObservableObject, IDisposable
{
	private readonly IBluetoothService _bluetoothService;
	private readonly FardriverFrameReassembler _reassembler;
	private readonly SynchronizationContext _syncContext;

	[ObservableProperty]
	private FardriverData _currentData;

	[ObservableProperty]
	private bool _hasData;

	[ObservableProperty]
	private bool _motorParametersAvailable;

	[ObservableProperty]
	private bool _powerCurveAvailable;

	[ObservableProperty]
	private bool _regenSettingsAvailable;

	[ObservableProperty]
	private bool _driveModeSettingsAvailable;

	[ObservableProperty]
	private int _framesAccepted;

	[ObservableProperty]
	private int _framesRejectedBadCrc;

	[ObservableProperty]
	private int _framesRejectedMalformed;

	public ConfigurationViewModel(IBluetoothService bluetoothService, FardriverFrameReassembler reassembler)
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

		if (!_reassembler.TryIngest(data, out var info))
		{
			UpdateDiagnostics();
			return;
		}

		// Refresh settings only when a settings (non-telemetry) frame arrives.
		// Telemetry frames (E2/E8/EE) update the dashboard continuously; we don't
		// need to re-evaluate settings availability on every dashboard tick.
		if (info.IsTelemetry)
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
				UpdateSectionAvailability();
				UpdateDiagnostics();
			}, null);
		}
		else
		{
			CurrentData = snapshot;
			HasData = true;
			UpdateSectionAvailability();
			UpdateDiagnostics();
		}
	}

	private void UpdateSectionAvailability()
	{
		// Motor Parameters: Addr06, 0C, 12, 18, 24, 2A
		MotorParametersAvailable =
			HasAddress(0x06) && HasAddress(0x0C) && HasAddress(0x12) &&
			HasAddress(0x18) && HasAddress(0x24) && HasAddress(0x2A);

		// Power Curve: Addr12 (LD), 18 (LQ), 06 (FAIF), 69 (LmtSpeed), 88, 8E
		PowerCurveAvailable =
			HasAddress(0x12) && HasAddress(0x18) && HasAddress(0x06) &&
			HasAddress(0x69) && HasAddress(0x88) && HasAddress(0x8E);

		// Regen Settings: Addr30, 18 (BattRatedCap), 2A (FreeThrottle), 8E, 94, 9A
		RegenSettingsAvailable =
			HasAddress(0x30) && HasAddress(0x18) && HasAddress(0x2A) &&
			HasAddress(0x8E) && HasAddress(0x94) && HasAddress(0x9A);

		// Drive Mode Settings: Addr24, 2A, 30
		DriveModeSettingsAvailable =
			HasAddress(0x24) && HasAddress(0x2A) && HasAddress(0x30);
	}

	private bool HasAddress(byte addr) => _reassembler.HasAddress(addr);

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
