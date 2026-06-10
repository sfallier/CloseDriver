using System;
using Xunit;
using CloseDriver.Core.Protocol;

namespace CloseDriver.Tests;

public class FarDriverProtocolParserTests
{
    [Fact]
    public void Parse_ThrowsArgumentException_WhenPayloadIsTooSmall()
    {
        // Arrange
        var payload = new byte[511];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => FarDriverProtocolParser.Parse(payload));
    }

    [Fact]
    public void Parse_ParsesValidPayload()
    {
        // Arrange
        var payload = new byte[512];
        
        // Populate some known byte positions based on the C++ struct reverse engineering

        // AddrE8 (Offset 464)
        // byte 2-3 (464 + 2 = 466) is deci_volts
        // Let's set it to 720 (0x02D0) which is 72.0 Volts
        payload[466] = 0xD0; // Little Endian LSB
        payload[467] = 0x02; // Little Endian MSB

        // Addr0C (Offset 0x0C * 2 = 24)
        // ZeroBattCoeff (Offset 24 + 2 = 26)
        payload[26] = 0x58; // 600 -> 0x0258
        payload[27] = 0x02;
        
        // FullBattCoeff (Offset 24 + 4 = 28)
        payload[28] = 0x48; // 840 -> 0x0348
        payload[29] = 0x03;

        // AddrE2 (Offset 452)
        // MeasureSpeed (Offset 452 + 8 = 460)
        payload[460] = 0x58; // 600 -> 0x0258
        payload[461] = 0x02;

        // AddrD0 (Offset 416)
        // WheelRatio (Offset 416 + 6 = 422)
        payload[422] = 100;
        // WheelRadius (Offset 416 + 7 = 423)
        payload[423] = 10;
        // WheelWidth (Offset 416 + 9 = 425)
        payload[425] = 90;
        // RateRatio (Offset 416 + 10 = 426)
        payload[426] = 0xE8; // 1000 -> 0x03E8
        payload[427] = 0x03;

        // Act
        var data = FarDriverProtocolParser.Parse(payload);

        // Assert
        Assert.Equal(720, data.DeciVolts);
        Assert.Equal(72.0f, data.Voltage, 0.1f);
        Assert.Equal(600, data.ZeroBattCoeff);
        Assert.Equal(840, data.FullBattCoeff);

        // Battery Percentage: (720 - 600) / (840 - 600) * 100 = 120 / 240 * 100 = 50%
        Assert.Equal(50.0f, data.BatteryPercentage, 0.1f);

        Assert.Equal(600, data.MeasureSpeed);
        Assert.Equal(100, data.WheelRatio);
        Assert.Equal(10, data.WheelRadius);
        Assert.Equal(90, data.WheelWidth);
        Assert.Equal(1000, data.RateRatio);

        // SpeedKph
        // 600 * (0.00376991136 * (10 * 1270 + 90 * 100) / 1000)
        // = 600 * (0.00376991136 * (12700 + 9000) / 1000)
        // = 600 * (0.00376991136 * 21700 / 1000)
        // = 600 * (0.00376991136 * 21.7)
        // = 600 * 0.081807076512
        // = 49.084...
        Assert.Equal(49.08f, data.SpeedKph, 0.01f);
    }
}
