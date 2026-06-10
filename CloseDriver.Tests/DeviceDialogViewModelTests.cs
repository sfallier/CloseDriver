using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Models;
using CloseDriver.Core.ViewModels;

namespace CloseDriver.Tests;

public class DeviceDialogViewModelTests
{
    [Fact]
    public void DeviceDiscovered_AddsToCollection()
    {
        // Arrange
        var mockService = new Mock<IBluetoothService>();
        var viewModel = new DeviceDialogViewModel(mockService.Object);
        var testDevice = new BluetoothDevice { Id = "TEST-1", Name = "Test Device", Rssi = -50 };

        // Act
        mockService.Raise(s => s.DeviceDiscovered += null, mockService.Object, testDevice);

        // Assert
        Assert.Single(viewModel.DiscoveredDevices);
        Assert.Equal("TEST-1", viewModel.DiscoveredDevices[0].Id);
    }

    [Fact]
    public void DeviceDiscovered_ExistingDevice_UpdatesRssi()
    {
        // Arrange
        var mockService = new Mock<IBluetoothService>();
        var viewModel = new DeviceDialogViewModel(mockService.Object);
        
        var initialDevice = new BluetoothDevice { Id = "TEST-1", Name = "Test Device", Rssi = -80 };
        mockService.Raise(s => s.DeviceDiscovered += null, mockService.Object, initialDevice);

        var updatedDevice = new BluetoothDevice { Id = "TEST-1", Name = "Test Device", Rssi = -40 };

        // Act
        mockService.Raise(s => s.DeviceDiscovered += null, mockService.Object, updatedDevice);

        // Assert
        Assert.Single(viewModel.DiscoveredDevices);
        Assert.Equal(-40, viewModel.DiscoveredDevices[0].Rssi);
    }

    [Fact]
    public void SelectDevice_SetsConfirmedDevice()
    {
        // Arrange
        var mockService = new Mock<IBluetoothService>();
        var viewModel = new DeviceDialogViewModel(mockService.Object);
        var testDevice = new BluetoothDevice { Id = "TEST-1", Name = "Test Device" };
        
        viewModel.SelectedDevice = testDevice;

        // Act
        viewModel.SelectDeviceCommand.Execute(null);

        // Assert
        Assert.Equal(testDevice, viewModel.ConfirmedDevice);
    }
}
