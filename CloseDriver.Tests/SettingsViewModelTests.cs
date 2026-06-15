using System.Threading;
using System.Threading.Tasks;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Protocol;
using CloseDriver.Core.ViewModels;
using Moq;
using Xunit;

namespace CloseDriver.Tests;

public class SettingsViewModelTests
{
	[Fact]
	public void OnDataReceived_ParsesDataAndUpdatesProperties()
	{
		// Arrange
		var mockService = new Mock<IBluetoothService>();
		var viewModel = new SettingsViewModel(mockService.Object);

		var payload = new byte[512];
		// DeciVolts (offset 466)
		payload[466] = 0xD0; // 720
		payload[467] = 0x02;

		// Act
		mockService.Raise(s => s.DataReceived += null, mockService.Object, payload);

		// Assert
		Assert.NotNull(viewModel.CurrentData);
		Assert.NotNull(viewModel.CurrentData.Value.Buffer);
		Assert.Equal(720, viewModel.CurrentData.Value.DeciVolts);
	}
}
