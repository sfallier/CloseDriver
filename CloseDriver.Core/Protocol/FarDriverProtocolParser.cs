using System;

namespace CloseDriver.Core.Protocol;

/// <summary>
/// Retained as a thin shim. The active ingestion path is FardriverFrameReassembler.
/// </summary>
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
