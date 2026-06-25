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

	// ── Addr0C (base = 0x0C * 2 = 24) ──────────────────────────────────────
	// Layout: byte0-1=PhaseOffset, byte2-3=ZeroBattCoeff, byte4-5=FullBattCoeff, ...
	public short ZeroBattCoeff => GetInt16(26);
	public short FullBattCoeff => GetInt16(28);

	// ── Addr12 (base = 0x12 * 2 = 36) ──────────────────────────────────────
	// Layout: byte0-1=LD, byte2-3=AlarmDelay, byte4=PolePairs, byte5=unk14b,
	//         byte6-7=MaxSpeed, byte8-9=RatedPower, byte10-11=RatedVoltage
	public byte   PolePairs => GetByte(40);
	public ushort MaxSpeed  => GetUInt16(42);

	// ── Addr18 (base = 0x18 * 2 = 48) ──────────────────────────────────────
	// Layout: byte0-1=RatedSpeed, byte2-3=MaxLineCurr(/4→A), byte4=cfg26l,
	//         byte5=cfg26h, byte6-7=LQ, byte8-9=BattRatedCap, byte10-11=IntRes
	public ushort RatedSpeed           => GetUInt16(48);
	public ushort MaxLineCurrRaw       => GetUInt16(50);
	public float  MaxLineCurrent       => MaxLineCurrRaw / 4f;
	public ushort BatteryRatedCapacity => GetUInt16(56);

	// ── Addr24 (base = 0x24 * 2 = 72) ──────────────────────────────────────
	// Layout: byte0=TimeMin, byte1=TimeSecond, byte2-3=HighVolProtect,
	//         byte4-5=CustomMaxLineCurr, byte6-7=CustomMaxPhaseCurr,
	//         byte8-9=BackSpeed (= LowSpeed in app UI), byte10-11=LowSpeed (creep)
	// App "LowSpeed" (5000) maps to the BackSpeed field at byte8-9 → buf[80]
	public ushort LowSpeed => GetUInt16(80);

	// ── Addr2A (base = 0x2A * 2 = 84) ──────────────────────────────────────
	// Layout: byte0-1=MidSpeed, byte2-3=Max_Dec, byte4=FreeThrottle, byte5=unk,
	//         byte6-7=MaxPhaseCurr(/4→A), byte8-9=SpeedAnalog, byte10-11=Max_Acc
	public ushort MiddleSpeed     => GetUInt16(84);
	public ushort MaxPhaseCurrRaw => GetUInt16(90);
	public float  MaxPhaseCurrent => MaxPhaseCurrRaw / 4f;

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
}
