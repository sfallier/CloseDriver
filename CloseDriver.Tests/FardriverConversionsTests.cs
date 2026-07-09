using CloseDriver.Core.Protocol;
using Xunit;

namespace CloseDriver.Tests;

public class FardriverConversionsTests
{
    [Theory]
    [InlineData(0, 0f)]
    [InlineData(720, 180f)]
    [InlineData(25, 6.25f)]
    [InlineData(1000, 250f)]
    [InlineData(1800, 450f)]
    public void CurrentRawToAmps_ReturnsAmps(ushort raw, float expected)
    {
        Assert.Equal(expected, FardriverConversions.CurrentRawToAmps(raw), 0.001f);
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(180f, 720)]
    [InlineData(6.25f, 25)]
    [InlineData(250f, 1000)]
    [InlineData(450f, 1800)]
    public void AmpsToCurrentRaw_RoundTrips(float amps, ushort expectedRaw)
    {
        Assert.Equal(expectedRaw, FardriverConversions.AmpsToCurrentRaw(amps));
    }

    [Theory]
    [InlineData(0, 0f)]
    [InlineData(720, 72f)]
    [InlineData(900, 90f)]
    public void VoltageRawToVolts_ReturnsVolts(ushort raw, float expected)
    {
        Assert.Equal(expected, FardriverConversions.VoltageRawToVolts(raw), 0.001f);
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(72f, 720)]
    [InlineData(90f, 900)]
    public void VoltsToVoltageRaw_RoundTrips(float volts, ushort expectedRaw)
    {
        Assert.Equal(expectedRaw, FardriverConversions.VoltsToVoltageRaw(volts));
    }

    [Theory]
    [InlineData(0, 0f)]
    [InlineData(18, 0.9f)]
    [InlineData(75, 3.75f)]
    [InlineData(100, 5f)]
    public void ThrottleRawToVolts_ReturnsVolts(byte raw, float expected)
    {
        Assert.Equal(expected, FardriverConversions.ThrottleRawToVolts(raw), 0.001f);
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(0.9f, 18)]
    [InlineData(3.75f, 75)]
    [InlineData(5f, 100)]
    public void VoltsToThrottleRaw_RoundTrips(float volts, byte expectedRaw)
    {
        Assert.Equal(expectedRaw, FardriverConversions.VoltsToThrottleRaw(volts));
    }

    [Theory]
    [InlineData(0, 0f)]
    [InlineData(306, 30.6f)]
    [InlineData(-90, -9f)]
    public void PhaseOffsetRawToDegrees_ReturnsDegrees(short raw, float expected)
    {
        Assert.Equal(expected, FardriverConversions.PhaseOffsetRawToDegrees(raw), 0.001f);
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(30.6f, 306)]
    [InlineData(-9f, -90)]
    public void DegreesToPhaseOffsetRaw_RoundTrips(float degrees, short expectedRaw)
    {
        Assert.Equal(expectedRaw, FardriverConversions.DegreesToPhaseOffsetRaw(degrees));
    }

    [Theory]
    [InlineData(0, 0.5f)]
    [InlineData(51, 40.34375f)]
    [InlineData(77, 60.65625f)]
    [InlineData(128, 100.5f)]
    public void RatioRawToPercent_ReturnsPercent(byte raw, float expected)
    {
        Assert.Equal(expected, FardriverConversions.RatioRawToPercent(raw), 0.0001f);
    }

    [Theory]
    [InlineData(40.34375f, 51)]
    [InlineData(60.65625f, 77)]
    [InlineData(100.5f, 128)]
    [InlineData(0.5f, 0)]
    public void RatioPercentToRaw_RoundTrips(float percent, byte expectedRaw)
    {
        Assert.Equal(expectedRaw, FardriverConversions.RatioPercentToRaw(percent));
    }

    [Fact]
    public void RoundTrip_RatioConversion_IsLossless()
    {
        for (byte raw = 0; raw < 255; raw++)
        {
            float percent = FardriverConversions.RatioRawToPercent(raw);
            byte roundTripped = FardriverConversions.RatioPercentToRaw(percent);
            Assert.Equal(raw, roundTripped);
        }
    }
}
