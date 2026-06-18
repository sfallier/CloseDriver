using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Protocol;
using CloseDriver.Core.ViewModels;
using Moq;
using Xunit;

namespace CloseDriver.Tests;

public class SettingsViewModelTests
{
	// Valid 16-byte frame: AA A4 D6 02 1E 00 00 00 00 00 00 00 00 00 12 E2
	// id=0x24 → addr=0xE8 → offset=464; data[2]=0xD6, data[3]=0x02 → DeciVolts=0x021E=542 at buf[466]
	private static readonly byte[] s_validFrame = new byte[16]
	{ 0xAA, 0xA4, 0xD6, 0x02, 0x1E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0xE2 };

	[Fact]
	public void OnDataReceived_ValidFrame_SetsHasDataTrue()
	{
		var mockService = new Mock<IBluetoothService>();
		var reassembler = new FardriverFrameReassembler();
		var viewModel = new SettingsViewModel(mockService.Object, reassembler);

		mockService.Raise(s => s.DataReceived += null, mockService.Object, s_validFrame);

		Assert.True(viewModel.HasData);
	}

	[Fact]
	public void OnDataReceived_ValidFrame_UpdatesCurrentData()
	{
		var mockService = new Mock<IBluetoothService>();
		var reassembler = new FardriverFrameReassembler();
		var viewModel = new SettingsViewModel(mockService.Object, reassembler);

		mockService.Raise(s => s.DataReceived += null, mockService.Object, s_validFrame);

		Assert.NotNull(viewModel.CurrentData.Buffer);
		// frame[2..3] = D6 02 → buf[464..465]; frame[4..5] = 1E 00 → buf[466..467]
		// DeciVolts reads buf[466..467] = 1E 00 = 30
		Assert.Equal(30, viewModel.CurrentData.DeciVolts);
	}

	[Fact]
	public void OnDataReceived_ValidFrame_IncrementsFramesAccepted()
	{
		var mockService = new Mock<IBluetoothService>();
		var reassembler = new FardriverFrameReassembler();
		var viewModel = new SettingsViewModel(mockService.Object, reassembler);

		mockService.Raise(s => s.DataReceived += null, mockService.Object, s_validFrame);

		Assert.Equal(1, viewModel.FramesAccepted);
	}

	[Fact]
	public void OnDataReceived_InvalidFrame_DoesNotSetHasData()
	{
		var mockService = new Mock<IBluetoothService>();
		var reassembler = new FardriverFrameReassembler();
		var viewModel = new SettingsViewModel(mockService.Object, reassembler);

		var badFrame = new byte[16];
		s_validFrame.CopyTo(badFrame, 0);
		badFrame[14] ^= 0xFF;
		mockService.Raise(s => s.DataReceived += null, mockService.Object, badFrame);

		Assert.False(viewModel.HasData);
		Assert.Equal(1, viewModel.FramesRejectedBadCrc);
	}
}
