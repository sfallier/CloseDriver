using System.Threading.Tasks;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Models;
using CloseDriver.Core.ViewModels;
using Moq;
using Xunit;

namespace CloseDriver.Tests;

public class MainViewModelTests
{
	[Fact]
	public async Task ScanForDevicesCommand_Currently_DoesNotStartScanning()
	{
		// Arrange
		var mockService = new Mock<IBluetoothService>();
		// Setup so adding event handlers doesn't cause issues
		mockService.SetupAllProperties();
		var viewModel = new MainViewModel(mockService.Object);

		// Act
		await viewModel.ScanForDevicesCommand.ExecuteAsync(null);

		// Assert - As per MainViewModel implementation, this is currently a placeholder
		// and does NOT call StartScanningAsync.
		mockService.Verify(s => s.StartScanningAsync(It.IsAny<BleScanMode>()), Times.Never);
	}
}
