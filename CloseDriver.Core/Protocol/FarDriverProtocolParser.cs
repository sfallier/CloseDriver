using System;
using System.Runtime.InteropServices;
using CloseDriver.Core.Protocol;

namespace CloseDriver.Core.Protocol;

public class FarDriverProtocolParser
{
	public static FardriverData Parse(byte[] payload)
	{
		if (payload == null || payload.Length < 512)
			throw new ArgumentException("Payload must be at least 512 bytes long.");

		var data = new FardriverData();
		data.Buffer = new byte[512];
		Array.Copy(payload, data.Buffer, 512);

		return data;
	}
}
