using Xunit;

namespace CloseDriver.Tests;

/// <summary>
/// RETIRED: The 512-byte direct-parse path (FarDriverProtocolParser) has been replaced
/// by FardriverFrameReassembler which ingests 16-byte BLE frames.
/// See FardriverCrcTests, FardriverFrameReassemblerTests, and FardriverDataHebTests.
/// </summary>
public class FarDriverProtocolParserTests
{
    [Fact]
    public void Retired_SeeReplacementTests()
    {
        // This test class is intentionally empty.
        // Coverage is provided by FardriverCrcTests, FardriverFrameReassemblerTests,
        // and FardriverDataHebTests.
    }
}
