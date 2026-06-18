using System.Runtime.InteropServices;

namespace CloseDriver.Core.Protocol;

/// <summary>
/// Port of the FarDriverData C++ struct from jackhumbert/fardriver-controllers.
/// Packed structure representing the 512-byte payload.
/// Note: C# doesn't support bitfields directly in StructLayout, so we must use
/// properties to mask and shift the underlying bytes/words.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 512)]
public struct FarDriverData
{
	// The C++ struct defines 26 separate 12-byte chunks (Addr00, Addr06, ..., AddrD0)
	// plus some skipped bytes. To correctly map this, we can either define the exact byte offsets
	// using FieldOffset, or map out the 512 byte array and use properties to read.
	// Given the complexity of the bitfields and offsets, a fixed size array buffer with accessor properties
	// is often the most robust way in C# to port C++ structs with bitfields.

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
	public byte[] Buffer;

	// Example properties based on the C++ struct

	// Addr00 (Offset 0x00 * 2 = 0)
	public short VolCoeff => GetInt16(0);
	public short Voltage2Coeff => GetInt16(2);
	public short PhaseACoeff => GetInt16(4);
	public short LineCoeff => GetInt16(6);
	public short PhaseCCoeff => GetInt16(8);
	public short SaveNum => GetInt16(10);

	// Addr06 (Offset 0x06 * 2 = 12)
	// byte 12 (2, 0x06)
	// uint8_t Arg2 : 1;
	// AntiTheftPulse : 2; 
	// uint8_t unk02a : 1;
	// uint8_t Protocol485 : 4;
	public byte Addr06_Byte2 => GetByte(12);
	public int Protocol485 => (Addr06_Byte2 >> 4) & 0x0F;

	// byte 13 (3)
	public byte Addr06_Byte3 => GetByte(13);
	public int MorseCode => Addr06_Byte3 & 0x7F;

	// byte 14 (4, 0x07)
	public byte SpeedKI => GetByte(14);
	public byte SpeedKP => GetByte(15);

	// byte 16 (6, 0x08)
	public byte ThrottleLow => GetByte(16);
	public byte ThrottleHigh => GetByte(17);

	// byte 18-19 (8-9, 0x09)
	public short FAIF => GetInt16(18);

	// byte 20-21 (10-11, 0x0A)
	public short CurveTime => GetInt16(20);

	// byte 22 (12, 0x0B)
	public byte Addr06_Byte12 => GetByte(22);
	public int BrakeConfig => Addr06_Byte12 & 0x0F;
	public int TempSensor => (Addr06_Byte12 >> 4) & 0x07;
	public bool PhaseExchange => ((Addr06_Byte12 >> 7) & 0x01) == 1;

	// Addr0C (Offset 0x0C * 2 = 24)
	public short PhaseOffset => GetInt16(24);
	public short ZeroBattCoeff => GetInt16(26);
	public short FullBattCoeff => GetInt16(28);

	// Addr12 (Offset 0x12 * 2 = 36)
	public short LD => GetInt16(36 + 2); // offset 2-3 of Addr12
	public ushort MaxSpeed => GetUInt16(36 + 6); // offset 6-7 (0x15)
	public ushort RatedPower => GetUInt16(36 + 8); // offset 8-9 (0x16)
	public ushort RatedVoltage => GetUInt16(36 + 10); // offset 10-11 (0x17)

	// Addr18 (Offset 0x18 * 2 = 48)
	public ushort RatedSpeed => GetUInt16(48 + 2);
	public ushort MaxLineCurr => GetUInt16(48 + 4);

	// AddrE2 (Offset 0xE2 * 2 = 452)
	public ushort MeasureSpeed => GetUInt16(452 + 8); // 8-9, 0xE5

	// AddrE8 (Offset 0xE8 * 2 = 464)
	public short DeciVolts => GetInt16(464 + 2); // 2-3 E8
	public short LineCurrent => GetInt16(464 + 6); // 6-7 EA

	// AddrF4 (Offset 0xF4 * 2 = 488)
	public short MotorTemp => GetInt16(488 + 0);

	// AddrD6 (Offset 0xD6 * 2 = 428)
	public short MosTemp => GetInt16(428 + 10); // 10-11

	// AddrD0 (Offset 0xD0 * 2 = 416)
	public byte WheelRatio => GetByte(416 + 6);
	public byte WheelRadius => GetByte(416 + 7);
	public byte WheelWidth => GetByte(416 + 9);
	public ushort RateRatio => GetUInt16(416 + 10);

	// AddrBE (Offset 0xBE * 2 = 380)
	public ushort TorqueCoeff => GetUInt16(380 + 10);

	// Helper Math Properties
	public float SpeedKph
	{
		get
		{
			if (RateRatio == 0)
				return 0;
			return MeasureSpeed * (0.00376991136f * (WheelRadius * 1270f + WheelWidth * WheelRatio) / RateRatio);
		}
	}

	public float MotorTempFahrenheit => MotorTemp * 9f / 5f + 32f;
	public float MosTempFahrenheit => MosTemp * 9f / 5f + 32f;
	public float BatteryPercentage => (FullBattCoeff - ZeroBattCoeff) == 0 ? 0 : 100f * (DeciVolts - ZeroBattCoeff) / (FullBattCoeff - ZeroBattCoeff);
	public float LineCurrentAmps => LineCurrent / 4f;
	public float Voltage => DeciVolts / 10f;

	// Utility methods for reading bytes assuming Little-Endian (which is standard for BLE and STM32)
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
