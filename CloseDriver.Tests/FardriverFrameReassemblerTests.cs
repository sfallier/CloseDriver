using CloseDriver.Core.Protocol;
using Xunit;

namespace CloseDriver.Tests;

public class FardriverFrameReassemblerTests
{
    // Valid frame: AA A4 D6 02 1E 00 00 00 00 00 00 00 00 00 12 E2
    // id = 0xA4 & 0x3F = 0x24; flashReadAddr[0x24] = 0xE8; offset = 0xE8 * 2 = 464
    private static readonly byte[] s_validFrameA4 = new byte[16]
    { 0xAA, 0xA4, 0xD6, 0x02, 0x1E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0xE2 };

    // Valid frame: AA A3 80 38 00 00 00 00 00 00 0B 00 FA FF 6F 11
    // id = 0xA3 & 0x3F = 0x23; flashReadAddr[0x23] = 0xE2; offset = 0xE2 * 2 = 452  (telemetry)
    private static readonly byte[] s_validFrameA3 = new byte[16]
    { 0xAA, 0xA3, 0x80, 0x38, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x00, 0xFA, 0xFF, 0x6F, 0x11 };

    [Fact]
    public void TryIngest_TooShort_ReturnsFalse_IncrementsMalformed()
    {
        var r = new FardriverFrameReassembler();
        bool result = r.TryIngest(new byte[15], out _);
        Assert.False(result);
        Assert.Equal(1, r.FramesRejectedMalformed);
        Assert.Equal(0, r.FramesAccepted);
    }

    [Fact]
    public void TryIngest_TooLong_ReturnsFalse_IncrementsMalformed()
    {
        var r = new FardriverFrameReassembler();
        bool result = r.TryIngest(new byte[17], out _);
        Assert.False(result);
        Assert.Equal(1, r.FramesRejectedMalformed);
    }

    [Fact]
    public void TryIngest_WrongHeader_ReturnsFalse_IncrementsMalformed()
    {
        var r = new FardriverFrameReassembler();
        var frame = new byte[16];
        s_validFrameA4.CopyTo(frame, 0);
        frame[0] = 0xBB;
        bool result = r.TryIngest(frame, out _);
        Assert.False(result);
        Assert.Equal(1, r.FramesRejectedMalformed);
    }

    [Fact]
    public void TryIngest_WrongFlags_ReturnsFalse_IncrementsMalformed()
    {
        var r = new FardriverFrameReassembler();
        var frame = new byte[16];
        s_validFrameA4.CopyTo(frame, 0);
        frame[1] = 0x24; // flags = 0, not 2
        bool result = r.TryIngest(frame, out _);
        Assert.False(result);
        Assert.Equal(1, r.FramesRejectedMalformed);
    }

    [Fact]
    public void TryIngest_BadCrc_ReturnsFalse_IncrementsBadCrc()
    {
        var r = new FardriverFrameReassembler();
        var frame = new byte[16];
        s_validFrameA4.CopyTo(frame, 0);
        frame[14] ^= 0xFF;
        bool result = r.TryIngest(frame, out _);
        Assert.False(result);
        Assert.Equal(1, r.FramesRejectedBadCrc);
        Assert.Equal(0, r.FramesAccepted);
    }

    [Fact]
    public void TryIngest_ValidFrame_WritesDataAtCorrectOffset()
    {
        var r = new FardriverFrameReassembler();
        // id = 0xA4 & 0x3F = 0x24 = 36; flashReadAddr[36] = 0xE8; offset = 0xE8 * 2 = 464
        bool result = r.TryIngest(s_validFrameA4, out var info);
        Assert.True(result);
        Assert.Equal(0x24, info.Id);
        Assert.Equal(0xE8, info.Address);
        Assert.True(info.IsTelemetry);
        Assert.Equal(1, r.FramesAccepted);

        var snapshot = r.Snapshot();
        // frame[2..13] = D6 02 1E 00 00 00 00 00 00 00 00 00 → buffer[464..475]
        Assert.Equal(0xD6, snapshot.Buffer[464]);
        Assert.Equal(0x02, snapshot.Buffer[465]);
        Assert.Equal(0x1E, snapshot.Buffer[466]);
    }

    [Fact]
    public void TryIngest_TelemetryFrame_MarkedAsTelemetry()
    {
        var r = new FardriverFrameReassembler();
        bool result = r.TryIngest(s_validFrameA3, out var info);
        Assert.True(result);
        Assert.Equal(0xE2, info.Address);
        Assert.True(info.IsTelemetry);
    }

    [Fact]
    public void TryIngest_ValidFrame_RaisesFrameIngestedEvent()
    {
        var r = new FardriverFrameReassembler();
        int eventCount = 0;
        r.FrameIngested += (_, _) => eventCount++;

        r.TryIngest(s_validFrameA4, out _);

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void TryIngest_InvalidFrame_DoesNotRaiseFrameIngestedEvent()
    {
        var r = new FardriverFrameReassembler();
        int eventCount = 0;
        r.FrameIngested += (_, _) => eventCount++;

        var frame = new byte[16];
        s_validFrameA4.CopyTo(frame, 0);
        frame[14] ^= 0xFF;
        r.TryIngest(frame, out _);

        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void Snapshot_ReturnsCopyNotReference()
    {
        var r = new FardriverFrameReassembler();
        r.TryIngest(s_validFrameA4, out _);

        var snap1 = r.Snapshot();
        r.TryIngest(s_validFrameA3, out _);
        var snap2 = r.Snapshot();

        // snap1 and snap2 are independent copies
        Assert.NotSame(snap1.Buffer, snap2.Buffer);
    }
}
