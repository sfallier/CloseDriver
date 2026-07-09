using System.IO;
using CloseDriver.Core.Protocol;
using Xunit;

namespace CloseDriver.Tests;

/// <summary>
/// Loads XeProS-Stock.heb and verifies every setting field in FardriverData against
/// the values displayed in the FarDriver app for that controller.
///
/// .heb layout (confirmed via HEB.bt):
///   26 settings structs × 12 bytes = 312 bytes, followed by CAN config.
///   Structs are packed consecutively in HEB.bt order (no addr*2 gaps).
///   Each chunk is copied into the FardriverData buffer at addr*2 to match
///   the live BLE reassembled buffer layout.
///
/// HEB.bt struct order → (address, addr*2):
///   Addr00(0x00,0), Addr06(0x06,12), Addr0C(0x0C,24), Addr12(0x12,36),
///   Addr18(0x18,48), Addr1E(0x1E,60), Addr24(0x24,72), Addr2A(0x2A,84),
///   Addr30(0x30,96), Addr63(0x63,198), Addr69(0x69,210), Addr7C(0x7C,248),
///   Addr82(0x82,260), Addr88(0x88,272), Addr8E(0x8E,284), Addr94(0x94,296),
///   Addr9A(0x9A,308), AddrA0(0xA0,320), AddrA6(0xA6,332), AddrAC(0xAC,344),
///   AddrB2(0xB2,356), AddrB8(0xB8,376), AddrBE(0xBE,380), AddrC4(0xC4,392),
///   AddrCA(0xCA,404), AddrD0(0xD0,416)
///
/// Verified app values (XeProS-Stock, 2026-06-16):
///   RatedSpeed=6800, PolePairs=4, MaxSpeed=9000, MaxLineCurrent=180A,
///   ThrottleLow=0.9V, BatteryRatedCapacity=55Ah, LowSpeedPhaseRatio≈60%,
///   LowSpeed=5000, MiddleSpeed=7000, StopBackCurrent=25A, MaxBackCurrent=30A,
///   WheelRatio=100, WheelRadius=18, WheelWidth=90, GearRatio=4.7
/// </summary>
public class FardriverDataHebTests
{
    private const string HebPath = @"c:\Users\Scott\Downloads\XeProS-Stock.heb";

    // HEB.bt struct order: (address, bufferOffset = address*2)
    // 26 structs × 12 bytes each = 312 bytes of settings in the .heb
    private static readonly byte[] s_hebAddresses = new byte[26]
    {
        0x00, 0x06, 0x0C, 0x12, 0x18, 0x1E, 0x24, 0x2A, 0x30,
        0x63, 0x69, 0x7C, 0x82, 0x88, 0x8E, 0x94, 0x9A, 0xA0,
        0xA6, 0xAC, 0xB2, 0xB8, 0xBE, 0xC4, 0xCA, 0xD0
    };

    private static FardriverData LoadHeb()
    {
        byte[] heb = File.ReadAllBytes(HebPath);
        var buffer = new byte[512];

        for (int i = 0; i < s_hebAddresses.Length; i++)
        {
            int hebOffset = i * 12;
            int bufOffset = s_hebAddresses[i] * 2;
            if (hebOffset + 12 > heb.Length || bufOffset + 12 > buffer.Length)
                break;
            for (int b = 0; b < 12; b++)
                buffer[bufOffset + b] = heb[hebOffset + b];
        }

        var data = new FardriverData();
        data.Buffer = buffer;
        return data;
    }

    [Fact]
    public void HebFile_Exists()
    {
        Assert.True(File.Exists(HebPath), $"HEB file not found at {HebPath}");
    }

    [Fact]
    public void RatedSpeed_Is6800()
    {
        var d = LoadHeb();
        Assert.Equal(6800, d.RatedSpeed);
    }

    [Fact]
    public void PolePairs_Is4()
    {
        var d = LoadHeb();
        Assert.Equal(4, d.PolePairs);
    }

    [Fact]
    public void MaxSpeed_Is9000()
    {
        var d = LoadHeb();
        Assert.Equal(9000, d.MaxSpeed);
    }

    [Fact]
    public void MaxLineCurrent_Is180A()
    {
        var d = LoadHeb();
        Assert.Equal(180f, d.MaxLineCurrent, 0.1f);
    }

    [Fact]
    public void ThrottleLow_Is0Point9V()
    {
        var d = LoadHeb();
        Assert.Equal(0.9f, d.ThrottleLow, 0.05f);
    }

    [Fact]
    public void BatteryRatedCapacity_Is55Ah()
    {
        var d = LoadHeb();
        Assert.Equal(55, d.BatteryRatedCapacity);
    }

    [Fact]
    public void LowSpeedPhaseRatio_Is60Percent()
    {
        var d = LoadHeb();
        Assert.Equal(60f, d.LowSpeedPhaseRatio, 1.0f);
    }

    [Fact]
    public void Mode2MaxSpeed_Is5000()
    {
        var d = LoadHeb();
        Assert.Equal(5000, d.Mode2MaxSpeed);
    }

    [Fact]
    public void Mode3MaxSpeed_Is7000()
    {
        var d = LoadHeb();
        Assert.Equal(7000, d.Mode3MaxSpeed);
    }

    [Fact]
    public void ReverseRpm_Is1000()
    {
        var d = LoadHeb();
        Assert.Equal(1000, d.ReverseRpm);
    }

    [Fact]
    public void StopBackCurrent_Is25A()
    {
        var d = LoadHeb();
        Assert.Equal(25, d.StopBackCurrent);
    }

    [Fact]
    public void MaxBackCurrent_Is30A()
    {
        var d = LoadHeb();
        Assert.Equal(30, d.MaxBackCurrent);
    }

    [Fact]
    public void WheelRatio_Is100()
    {
        var d = LoadHeb();
        Assert.Equal(100, d.WheelRatio);
    }

    [Fact]
    public void WheelRadius_Is18()
    {
        var d = LoadHeb();
        Assert.Equal(18, d.WheelRadius);
    }

    [Fact]
    public void WheelWidth_Is90()
    {
        var d = LoadHeb();
        Assert.Equal(90, d.WheelWidth);
    }

    [Fact]
    public void GearRatio_Is4Point7()
    {
        var d = LoadHeb();
        Assert.Equal(4.7f, d.GearRatio, 0.01f);
    }

    [Fact]
    public void PhaseOffset_Is30Point6()
    {
        var d = LoadHeb();
        Assert.Equal(30.6f, d.PhaseOffset, 0.1f);
    }

    [Fact]
    public void RatedVoltage_Is72V()
    {
        var d = LoadHeb();
        Assert.Equal(72.0f, d.RatedVoltage, 0.1f);
    }

    [Fact]
    public void MaxPhaseCurrent_Is450A()
    {
        var d = LoadHeb();
        Assert.Equal(450f, d.MaxPhaseCurrent, 0.1f);
    }

    [Fact]
    public void BoostLineCurrent_Is250A()
    {
        var d = LoadHeb();
        Assert.Equal(250f, d.BoostLineCurrentAmps, 0.1f);
    }

    [Fact]
    public void BoostPhaseCurrent_Is550A()
    {
        var d = LoadHeb();
        Assert.Equal(550f, d.BoostPhaseCurrentAmps, 0.1f);
    }

    [Fact]
    public void ThrottleAccelStep_Is224()
    {
        var d = LoadHeb();
        Assert.Equal(224, d.ThrottleAccelStep);
    }

    [Fact]
    public void ThrottleDecelStep_Is224()
    {
        var d = LoadHeb();
        Assert.Equal(224, d.ThrottleDecelStep);
    }

    [Fact]
    public void FreeThrottle_Is0()
    {
        var d = LoadHeb();
        Assert.Equal(0, d.FreeThrottle);
    }

    [Fact]
    public void LD_Is900()
    {
        var d = LoadHeb();
        Assert.Equal(900, d.LD);
    }

    [Fact]
    public void LQ_Is329()
    {
        var d = LoadHeb();
        Assert.Equal(329, d.LQ);
    }

    [Fact]
    public void FAIF_Is513()
    {
        var d = LoadHeb();
        Assert.Equal(513, d.FAIF);
    }

    [Fact]
    public void RPMSpeedLimit_Is9000()
    {
        var d = LoadHeb();
        Assert.Equal(9000, d.RPMSpeedLimit);
    }

    [Fact]
    public void MotorDirection_IsClockwise()
    {
        var d = LoadHeb();
        Assert.Equal(MotorDirection.Clockwise, d.MotorDirection);
    }

    [Fact]
    public void TempSensorType_IsNTC10K()
    {
        var d = LoadHeb();
        Assert.Equal(TempSensorType.NTC10K, d.TempSensorType);
    }

    [Fact]
    public void PhaseExchange_IsFalse()
    {
        var d = LoadHeb();
        Assert.False(d.PhaseExchange);
    }

    [Fact]
    public void ThrottleResponse_IsLinear()
    {
        var d = LoadHeb();
        Assert.Equal(ThrottleResponseType.Linear, d.ThrottleResponse);
    }

    [Fact]
    public void FollowMode_IsEabsWhenBrake()
    {
        var d = LoadHeb();
        Assert.Equal(FollowMode.EabsWhenBrake, d.FollowMode);
    }

    [Fact]
    public void PowerCurve_FirstAndLastPoints_MatchHeb()
    {
        var d = LoadHeb();
        Assert.Equal(75f, d.GetPowerCurvePercent(0), 1f);
        Assert.Equal(15f, d.GetPowerCurvePercent(19), 1f);
    }

    [Fact]
    public void RegenCurve_NegativePoints_MatchHeb()
    {
        var d = LoadHeb();
        Assert.Equal(-13f, d.GetRegenCurvePercent(0), 1f);
        // Last point in the Addr9A chunk is 0 in this stock file.
        Assert.Equal(0f, d.GetRegenCurvePercent(19), 1f);
    }
}
