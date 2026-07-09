namespace CloseDriver.Core.Protocol;

/// <summary>
/// Motor rotation direction for forward travel.
/// Manual §2.1.5: 0 = sprocket on the right, 1 = sprocket on the left.
/// </summary>
public enum MotorDirection : byte
{
	Clockwise = 0,
	CounterClockwise = 1,
}

/// <summary>
/// Motor temperature sensor type (Addr06 byte10 bits 4-6). Manual §2.1.2.
/// </summary>
public enum TempSensorType : byte
{
	None = 0,
	PTC = 1,
	NTC230K = 2,
	KTY84_130 = 3,
	CACU = 4,
	KTY83_122 = 5,
	NTC10K = 6,
	NTC100K = 7,
}

/// <summary>
/// Throttle response curve (Addr18 cfg26l bits 2-3).
/// 0 is "Line" in the FarDriver app — a bad translation of Linear.
/// </summary>
public enum ThrottleResponseType : byte
{
	Linear = 0,
	Sport = 1,
	Eco = 2,
}

/// <summary>
/// Regen trigger mode (Addr18 cfg26l bits 0-1) — the "Follow" setting in the
/// FarDriver app. Determines when regenerative braking (EABS) is applied.
/// </summary>
public enum FollowMode : byte
{
	Follow = 0,
	Disabled = 1,
	EabsWhenBrake = 2,
	EabsWhenReleaseThrottle = 3,
}
