using System.Runtime.InteropServices;

namespace CloseDriver.Core.Protocol;

/// <summary>
/// Wraps the 512-byte FardriverData buffer assembled by FardriverFrameReassembler.
/// All byte offsets: bufferOffset = address * 2 (little-endian, matching STM32 layout).
/// Offsets verified against XeProS-Stock.heb and fardriver.hpp from jackhumbert/fardriver-controllers.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 512)]
public struct FardriverData
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
	public byte[] Buffer;

	// ── Addr06 (base = 0x06 * 2 = 12) ──────────────────────────────────────
	// Layout: byte0=cfg06l, byte1=MorseCode, byte2=SpeedKI, byte3=SpeedKP,
	//         byte4=ThrottleLow(/20→V), byte5=ThrottleHigh(/20→V),
	//         byte6-7=FAIF, byte8-9=CurveTime, byte10-11=cfg0Bl/cfg0Bh
	public float ThrottleLow  => GetByte(16) / 20f;
	public float ThrottleHigh => GetByte(17) / 20f;
	public short FAIF         => GetInt16(18);

	// Motor-direction and temperature-sensor bitfields live in Addr06 byte10-11.
	public MotorDirection MotorDirection     => (MotorDirection)GetBit(23, 7, 1);
	public TempSensorType TempSensorType     => (TempSensorType)GetBit(22, 4, 3);
	public bool           PhaseExchange      => GetBit(22, 7, 1) == 1;

	// ── Addr0C (base = 0x0C * 2 = 24) ──────────────────────────────────────
	// Layout: byte0-1=PhaseOffset, byte2-3=ZeroBattCoeff, byte4-5=FullBattCoeff, ...
	public float PhaseOffset   => GetInt16(24) / 10f;
	public short ZeroBattCoeff => GetInt16(26);
	public short FullBattCoeff => GetInt16(28);

	// ── Addr12 (base = 0x12 * 2 = 36) ──────────────────────────────────────
	// Layout: byte0-1=LD, byte2-3=AlarmDelay, byte4=PolePairs, byte5=unk14b,
	//         byte6-7=MaxSpeed, byte8-9=RatedPower, byte10-11=RatedVoltage
	public ushort LD          => GetUInt16(36);
	public byte   PolePairs   => GetByte(40);
	public ushort MaxSpeed    => GetUInt16(42);
	public float  RatedVoltage => GetUInt16(46) / 10f;

	// ── Addr18 (base = 0x18 * 2 = 48) ──────────────────────────────────────
	// Layout: byte0-1=RatedSpeed, byte2-3=MaxLineCurr(/4→A), byte4=cfg26l,
	//         byte5=cfg26h, byte6-7=LQ, byte8-9=BatteryRatedCap, byte10-11=IntRes
	// (HPP comment offsets are off by 2; XeProS-Stock.heb has LQ at 54, BatteryRatedCap at 56, IntRes at 58.)
	public ushort RatedSpeed           => GetUInt16(48);
	public ushort MaxLineCurrRaw       => GetUInt16(50);
	public float  MaxLineCurrent       => MaxLineCurrRaw / 4f;
	public ushort LQ                   => GetUInt16(54);
	public ushort BatteryRatedCapacity => GetUInt16(56);
	public ushort IntRes               => GetUInt16(58);

	// cfg26l (buf[52]) bitfields.
	public FollowMode         FollowMode         => (FollowMode)GetBit(52, 0, 2);
	public ThrottleResponseType ThrottleResponse => (ThrottleResponseType)GetBit(52, 2, 2);

	// ── Addr24 (base = 0x24 * 2 = 72) ──────────────────────────────────────
	// Layout: byte0=TimeMin, byte1=TimeSecond, byte2-3=HighVolProtect,
	//         byte4-5=CustomMaxLineCurr, byte6-7=CustomMaxPhaseCurr,
	//         byte8-9=BackSpeed, byte10-11=LowSpeed
	public ushort BoostLineCurrent  => GetUInt16(76);
	public float  BoostLineCurrentAmps => BoostLineCurrent / 4f;
	public ushort BoostPhaseCurrent => GetUInt16(78);
	public float  BoostPhaseCurrentAmps => BoostPhaseCurrent / 4f;
	public ushort Mode2MaxSpeed     => GetUInt16(80); // BackSpeed in hpp; app labels Mode 2 max speed
	public ushort ReverseRpm        => GetUInt16(82); // LowSpeed in hpp; app labels Reverse RPM

	// ── Addr2A (base = 0x2A * 2 = 84) ──────────────────────────────────────
	// Layout: byte0-1=MidSpeed, byte2-3=Max_Dec, byte4=FreeThrottle, byte5=unk,
	//         byte6-7=MaxPhaseCurr(/4→A), byte8-9=SpeedAnalog, byte10-11=Max_Acc
	public ushort Mode3MaxSpeed   => GetUInt16(84); // MidSpeed
	public ushort ThrottleDecelStep => GetUInt16(86); // Max_Dec
	public byte   FreeThrottle    => GetByte(88);
	public ushort MaxPhaseCurrRaw => GetUInt16(90);
	public float  MaxPhaseCurrent => MaxPhaseCurrRaw / 4f;
	public ushort ThrottleAccelStep => GetUInt16(94); // Max_Acc

	// ── Addr30 (base = 0x30 * 2 = 96) ──────────────────────────────────────
	// Layout: byte0-1=StopBackCurr, byte2-3=MaxBackCurr,
	//         byte4=LowSpeedLineCurr, byte5=MidSpeedLineCurr,
	//         byte6=LowSpeedPhaseCurr, byte7=MidSpeedPhaseCurr, ...
	// Ratio formula: (rawByte * 100 / 128.0) + 0.5
	public ushort StopBackCurrent  => GetUInt16(96);
	public ushort MaxBackCurrent   => GetUInt16(98);
	public float  LowSpeedLineRatio  => GetByte(100) * 100f / 128f + 0.5f;
	public float  MidSpeedLineRatio  => GetByte(101) * 100f / 128f + 0.5f;
	public float  LowSpeedPhaseRatio => GetByte(102) * 100f / 128f + 0.5f;
	public float  MidSpeedPhaseRatio => GetByte(103) * 100f / 128f + 0.5f;

	// ── Addr69 (base = 0x69 * 2 = 210) ─────────────────────────────────────
	// Layout: pin nibbles at 0..5, word 0x6C=LmtSpeed, word 0x6D=DistanceLSB
	public ushort RPMSpeedLimit => GetUInt16(216); // LmtSpeed

	// ── AddrD0 (base = 0xD0 * 2 = 416) ──────────────────────────────────────
	// Layout (verified against XeProS-Stock.heb AddrD0 chunk at heb[300..311]):
	//   byte0=Data0, byte1=Data1, byte2=BMQHALL, byte3=AVGPower,
	//   byte4=WheelRatio, byte5=WheelRadius, byte6=AVGSpeed, byte7=WheelWidth,
	//   byte8-9=RateRatio(/1000=GearRatio), byte10-11=OneCommCfg
	public byte   WheelRatio => GetByte(420);
	public byte   WheelRadius => GetByte(421);
	public byte   WheelWidth  => GetByte(423);
	public ushort RateRatio   => GetUInt16(424);
	public float  GearRatio   => RateRatio / 1000f;

	// ── AddrE2 (base = 0xE2 * 2 = 452) — Live telemetry ────────────────────
	// MeasureSpeed: uint16 at struct byte6-7 → buf[458]  (hpp comment "// 8-9" = BLE frame byte pos; struct byte = frame byte - 2)
	public ushort MeasureSpeed => GetUInt16(458);

	// ── AddrE8 (base = 0xE8 * 2 = 464) — Live telemetry ────────────────────
	// Layout confirmed from live log frame AA A4 D7 02 20 00 00 00 00 00 00 00 00 00:
	//   frame[2..13] → buf[464..475]
	//   DeciVolts:   int16 at struct byte0-1 → buf[464], /10 → volts
	//   LineCurrent: int16 at struct byte4-5 → buf[468], /4 → amps
	//   (hpp "// 2-3" and "// 6-7" are BLE frame byte positions; struct byte = frame byte - 2)
	public short DeciVolts   => GetInt16(464);
	public short LineCurrent => GetInt16(468);

	// ── AddrF4 (base = 0xF4 * 2 = 488) — Live telemetry ────────────────────
	// MotorTemp: int16 at byte0 → buf[488], raw °C
	public short MotorTemp => GetInt16(488);

	// ── AddrD6 (base = 0xD6 * 2 = 428) — Live telemetry ────────────────────
	// MosTemp: int16 at byte10 → buf[438], raw °C
	public short MosTemp => GetInt16(438);

	// ── Power & Regen curves ─────────────────────────────────────────────────
	// 20 RPM sample points: 0, 500, 1000, ..., 9000, 9500 (Max)
	private static readonly ushort[] s_rpmPoints = new ushort[20]
	{
		0, 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500,
		5000, 5500, 6000, 6500, 7000, 7500, 8000, 8500, 9000, 9500
	};

	/// <summary>Power curve percentage at the 20 fixed RPM points (current limit %).</summary>
	public float GetPowerCurvePercent(int index)
	{
		if (index < 0 || index >= 20)
			return 0;
		return s_powerCurveOffsets[index] >= 0 ? GetByte((int)s_powerCurveOffsets[index]) : 0;
	}

	/// <summary>Regenerative-braking curve percentage at the 20 fixed RPM points.</summary>
	public float GetRegenCurvePercent(int index)
	{
		if (index < 0 || index >= 20)
			return 0;
		return s_regenCurveOffsets[index] >= 0 ? (sbyte)GetByte((int)s_regenCurveOffsets[index]) : 0;
	}

	public ushort GetPowerCurveRPM(int index)
	{
		if (index < 0 || index >= 20)
			return 0;
		return s_rpmPoints[index];
	}

	// Look-up tables so the indexed accessors avoid loops and branches at runtime.
	// Addr88 = 0x88*2 = 272. Addr8E = 0x8E*2 = 284. Addr94 = 0x94*2 = 296. Addr9A = 0x9A*2 = 308.
	private static readonly short[] s_powerCurveOffsets = new short[20]
	{
		272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283,
		284, 285, 286, 287, 288, 289, 290, 291
	};

	private static readonly short[] s_regenCurveOffsets = new short[20]
	{
		292, 293, 294, 295, // Addr8E nratio_0..3
		296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, // Addr94 nratio_4..15
		308, 309, 310, 311  // Addr9A nratio_16..19
	};

	// ── Dashboard computed properties ────────────────────────────────────────
	public float Voltage             => DeciVolts / 10f;
	public float LineCurrentAmps     => LineCurrent / 4f;
	public float MotorTempFahrenheit => MotorTemp * 9f / 5f + 32f;
	public float MosTempFahrenheit   => MosTemp * 9f / 5f + 32f;

	public float BatteryPercentage
	{
		get
		{
			int range = FullBattCoeff - ZeroBattCoeff;
			if (range <= 0 || ZeroBattCoeff == 0)
				return float.NaN;
			return 100f * (DeciVolts - ZeroBattCoeff) / range;
		}
	}

	public float SpeedKph
	{
		get
		{
			if (RateRatio == 0)
				return 0;
			return MeasureSpeed * (0.00376991136f * (WheelRadius * 1270f + WheelWidth * WheelRatio) / RateRatio);
		}
	}

	// ── Utility ──────────────────────────────────────────────────────────────
	private byte GetByte(int offset)
	{
		if (Buffer == null || offset >= Buffer.Length)
			return 0;
		return Buffer[offset];
	}

	private short GetInt16(int offset)
	{
		if (Buffer == null || offset + 1 >= Buffer.Length)
			return 0;
		return (short)(Buffer[offset] | (Buffer[offset + 1] << 8));
	}

	private ushort GetUInt16(int offset)
	{
		if (Buffer == null || offset + 1 >= Buffer.Length)
			return 0;
		return (ushort)(Buffer[offset] | (Buffer[offset + 1] << 8));
	}

	private byte GetBit(int offset, int shift, int count)
	{
		byte value = GetByte(offset);
		return (byte)((value >> shift) & ((1 << count) - 1));
	}
}
