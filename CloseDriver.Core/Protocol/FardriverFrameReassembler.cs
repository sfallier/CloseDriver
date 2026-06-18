using System;

namespace CloseDriver.Core.Protocol;

/// <summary>
/// Ingests 16-byte BLE notification frames, translates id→address via flashReadAddr,
/// validates CRC, and assembles the 512-byte FardriverData buffer incrementally.
/// </summary>
public class FardriverFrameReassembler
{
    /// <summary>
    /// The 55-entry flash_read_addr table from fardriver_message.hpp.
    /// Index = id (frame[1] &amp; 0x3F). Value = address; byte offset in buffer = address * 2.
    /// </summary>
    private static readonly byte[] s_flashReadAddr = new byte[55]
    {
        0xE2, 0xE8, 0xEE, 0x00, 0x06, 0x0C, 0x12,
        0xE2, 0xE8, 0xEE, 0x18, 0x1E, 0x24, 0x2A,
        0xE2, 0xE8, 0xEE, 0x30, 0x5D, 0x63, 0x69,
        0xE2, 0xE8, 0xEE, 0x7C, 0x82, 0x88, 0x8E,
        0xE2, 0xE8, 0xEE, 0x94, 0x9A, 0xA0, 0xA6,
        0xE2, 0xE8, 0xEE, 0xAC, 0xB2, 0xB8, 0xBE,
        0xE2, 0xE8, 0xEE, 0xC4, 0xCA, 0xD0,
        0xE2, 0xE8, 0xEE, 0xD6, 0xDC, 0xF4, 0xFA
    };

    /// <summary>
    /// Telemetry address markers — frames whose address is one of these are live
    /// telemetry (not settings) and still update the buffer every cycle.
    /// </summary>
    private static readonly byte[] s_telemetryAddrs = new byte[] { 0xE2, 0xE8, 0xEE };

    private readonly byte[] _buffer = new byte[512];
    private readonly object _lock = new object();

    public int FramesAccepted { get; private set; }
    public int FramesRejectedBadCrc { get; private set; }
    public int FramesRejectedMalformed { get; private set; }

    /// <summary>
    /// Raised on the calling thread after each successfully ingested frame.
    /// </summary>
    public event EventHandler<FrameInfo> FrameIngested;

    /// <summary>
    /// Attempts to ingest a single 16-byte BLE notification frame.
    /// </summary>
    /// <returns>True if the frame was valid and written to the buffer.</returns>
    public bool TryIngest(ReadOnlySpan<byte> frame, out FrameInfo info)
    {
        info = default;

        if (frame.Length != 16 || frame[0] != 0xAA || (frame[1] >> 6) != 2)
        {
            FramesRejectedMalformed++;
            return false;
        }

        if (!FardriverCrc.Validate(frame))
        {
            FramesRejectedBadCrc++;
            return false;
        }

        int id = frame[1] & 0x3F;
        if (id >= s_flashReadAddr.Length)
        {
            FramesRejectedMalformed++;
            return false;
        }

        byte addr = s_flashReadAddr[id];
        int offset = addr * 2;

        if (offset + 12 > _buffer.Length)
        {
            FramesRejectedMalformed++;
            return false;
        }

        bool isTelemetry = false;
        for (int i = 0; i < s_telemetryAddrs.Length; i++)
        {
            if (s_telemetryAddrs[i] == addr)
            {
                isTelemetry = true;
                break;
            }
        }

        lock (_lock)
        {
            for (int i = 0; i < 12; i++)
                _buffer[offset + i] = frame[2 + i];
            FramesAccepted++;
        }

        info = new FrameInfo(id, addr, isTelemetry);
        FrameIngested?.Invoke(this, info);
        return true;
    }

    /// <summary>
    /// Returns a new FardriverData wrapping a snapshot copy of the current buffer.
    /// Thread-safe: takes the lock while copying.
    /// </summary>
    public FardriverData Snapshot()
    {
        var copy = new byte[512];
        lock (_lock)
        {
            Array.Copy(_buffer, copy, 512);
        }
        var data = new FardriverData();
        data.Buffer = copy;
        return data;
    }
}

/// <summary>
/// Metadata about a successfully ingested frame.
/// </summary>
public readonly struct FrameInfo
{
    public int Id { get; }
    public byte Address { get; }
    public bool IsTelemetry { get; }

    public FrameInfo(int id, byte address, bool isTelemetry)
    {
        Id = id;
        Address = address;
        IsTelemetry = isTelemetry;
    }
}
