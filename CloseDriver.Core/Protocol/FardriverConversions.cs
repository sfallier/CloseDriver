using System;

namespace CloseDriver.Core.Protocol;

/// <summary>
/// Pure bidirectional conversion functions between raw controller register values
/// and display units. These are math only — they do not write to the controller.
/// </summary>
public static class FardriverConversions
{
	// --- Current ---

	public static float CurrentRawToAmps(ushort raw) => raw / 4f;

	public static ushort AmpsToCurrentRaw(float amps) => (ushort)Math.Round(amps * 4f);

	// --- Voltage ---

	public static float VoltageRawToVolts(ushort raw) => raw / 10f;

	public static ushort VoltsToVoltageRaw(float volts) => (ushort)Math.Round(volts * 10f);

	// --- Throttle voltage ---

	public static float ThrottleRawToVolts(byte raw) => raw / 20f;

	public static byte VoltsToThrottleRaw(float volts) => (byte)Math.Round(volts * 20f);

	// --- Phase offset ---

	public static float PhaseOffsetRawToDegrees(short raw) => raw / 10f;

	public static short DegreesToPhaseOffsetRaw(float degrees) => (short)Math.Round(degrees * 10f);

	// --- Drive-mode ratio (raw byte -> percentage) ---
	// The firmware uses: display = raw * 100 / 128 + 0.5
	// Inverse: raw = (display - 0.5) * 128 / 100
	// Kept as float for lossless round-trip.

	public static float RatioRawToPercent(byte raw) => raw * 100f / 128f + 0.5f;

	public static byte RatioPercentToRaw(float percent) => (byte)Math.Round((percent - 0.5f) * 128f / 100f);

	// --- Identity helpers ---

	public static ushort IdentityUShort(ushort raw) => raw;
	public static ushort IdentityUShort(float value) => (ushort)Math.Round(value);

	public static short IdentityShort(short raw) => raw;
	public static short IdentityShort(float value) => (short)Math.Round(value);

	public static byte IdentityByte(byte raw) => raw;
	public static byte IdentityByte(float value) => (byte)Math.Round(value);
}
