using CloseDriver.Core.Protocol;
using Xunit;

namespace CloseDriver.Tests;

/// <summary>
/// Validates FardriverCrc using 5 real frames captured from CONTROLDM94F_Log_20260616_225247.txt.
/// Each frame: bytes 0..13 are the input; bytes 14..15 are the expected CRC (hi, lo).
/// </summary>
public class FardriverCrcTests
{
    // Frame: AA A3 80 38 00 00 00 00 00 00 0B 00 FA FF 6F 11
    private static readonly byte[] s_frame1 = new byte[16]
    { 0xAA, 0xA3, 0x80, 0x38, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x00, 0xFA, 0xFF, 0x6F, 0x11 };

    // Frame: AA A4 D6 02 1E 00 00 00 00 00 00 00 00 00 12 E2
    private static readonly byte[] s_frame2 = new byte[16]
    { 0xAA, 0xA4, 0xD6, 0x02, 0x1E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0xE2 };

    // Frame: AA A5 01 39 00 00 00 00 01 00 00 01 00 00 CD 43
    private static readonly byte[] s_frame3 = new byte[16]
    { 0xAA, 0xA5, 0x01, 0x39, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0xCD, 0x43 };

    // Frame: AA A6 00 00 B0 00 00 00 00 00 00 00 00 00 B3 D9
    private static readonly byte[] s_frame4 = new byte[16]
    { 0xAA, 0xA6, 0x00, 0x00, 0xB0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB3, 0xD9 };

    // Frame: AA AF 08 61 02 00 64 12 00 5A 5C 12 39 15 1F F2
    private static readonly byte[] s_frame5 = new byte[16]
    { 0xAA, 0xAF, 0x08, 0x61, 0x02, 0x00, 0x64, 0x12, 0x00, 0x5A, 0x5C, 0x12, 0x39, 0x15, 0x1F, 0xF2 };

    [Fact]
    public void Compute_Frame1_MatchesTrailingBytes()
    {
        var (hi, lo) = FardriverCrc.Compute(s_frame1, 14);
        Assert.Equal(s_frame1[14], hi);
        Assert.Equal(s_frame1[15], lo);
    }

    [Fact]
    public void Compute_Frame2_MatchesTrailingBytes()
    {
        var (hi, lo) = FardriverCrc.Compute(s_frame2, 14);
        Assert.Equal(s_frame2[14], hi);
        Assert.Equal(s_frame2[15], lo);
    }

    [Fact]
    public void Compute_Frame3_MatchesTrailingBytes()
    {
        var (hi, lo) = FardriverCrc.Compute(s_frame3, 14);
        Assert.Equal(s_frame3[14], hi);
        Assert.Equal(s_frame3[15], lo);
    }

    [Fact]
    public void Compute_Frame4_MatchesTrailingBytes()
    {
        var (hi, lo) = FardriverCrc.Compute(s_frame4, 14);
        Assert.Equal(s_frame4[14], hi);
        Assert.Equal(s_frame4[15], lo);
    }

    [Fact]
    public void Compute_Frame5_MatchesTrailingBytes()
    {
        var (hi, lo) = FardriverCrc.Compute(s_frame5, 14);
        Assert.Equal(s_frame5[14], hi);
        Assert.Equal(s_frame5[15], lo);
    }

    [Fact]
    public void Validate_ValidFrame_ReturnsTrue()
    {
        Assert.True(FardriverCrc.Validate(s_frame1));
        Assert.True(FardriverCrc.Validate(s_frame2));
        Assert.True(FardriverCrc.Validate(s_frame5));
    }

    [Fact]
    public void Validate_CorruptedByte_ReturnsFalse()
    {
        var corrupt = new byte[16];
        s_frame1.CopyTo(corrupt, 0);
        corrupt[5] ^= 0xFF;
        Assert.False(FardriverCrc.Validate(corrupt));
    }

    [Fact]
    public void Validate_TooShort_ReturnsFalse()
    {
        Assert.False(FardriverCrc.Validate(new byte[15]));
    }
}
