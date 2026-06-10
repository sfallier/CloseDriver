using Moq;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.ViewModels;

namespace CloseDriver.Tests;

public class MainViewModelTests
{
    [Fact]
    public async Task ScanForDevicesCommand_StartsScanning()
    {
        // Arrange
        var mockService = new Mock<IBluetoothService>();
        var viewModel = new MainViewModel(mockService.Object);

        // Act
        await viewModel.ScanForDevicesCommand.ExecuteAsync(null);

        // Assert
        mockService.Verify(s => s.StartScanningAsync(), Times.Once);
    }
}
