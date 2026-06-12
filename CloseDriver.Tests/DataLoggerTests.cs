using System;
using System.IO;
using System.Threading.Tasks;
using CloseDriver.Core.Services;
using Xunit;

namespace CloseDriver.Tests;

public class DataLoggerTests : IDisposable
{
	private readonly string _testFilePath;

	public DataLoggerTests()
	{
		_testFilePath = Path.Combine(Path.GetTempPath(), $"DataLoggerTest_{Guid.NewGuid()}.log");
	}

	[Fact]
	public async Task LogDataAsync_WritesCorrectlyFormattedHex()
	{
		// Arrange
		using var logger = new DataLogger(_testFilePath);
		logger.Start();

		var testDataRx = new byte[] { 0x5A, 0x01, 0xFF, 0x00, 0x1B };
		var testDataTx = new byte[] { 0xA5, 0x02, 0xEE };

		// Act
		await logger.LogDataAsync(testDataRx, isTransmit: false);
		await logger.LogDataAsync(testDataTx, isTransmit: true);

		logger.Stop(); // Flush and close

		// Assert
		Assert.True(File.Exists(_testFilePath));

		var lines = await File.ReadAllLinesAsync(_testFilePath);
		Assert.Equal(2, lines.Length);

		// Asserting format like: [2023-10-27 10:00:00.000] RX: 5A 01 FF 00 1B
		Assert.Contains("RX: 5A 01 FF 00 1B", lines[0]);
		Assert.Contains("TX: A5 02 EE", lines[1]);
	}

	public void Dispose()
	{
		if (File.Exists(_testFilePath))
		{
			File.Delete(_testFilePath);
		}
	}
}
