using Shiny.CallDirectory.Infrastructure;

namespace Shiny.CallDirectory.Tests;


public class CallScreeningTests
{
    [Theory]
    [InlineData("+18005550100", 1, 18005550100L)]
    [InlineData("8005550100", 1, 18005550100L)]
    [InlineData("+1 (800) 555-0100", 1, 18005550100L)]
    [InlineData("02079460018", 44, 442079460018L)]
    public void Normalize_IncomingHandles(string handle, int countryCode, long expected)
        => Assert.Equal(expected, CallScreening.Normalize(handle, countryCode));


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("anonymous")]
    public void Normalize_WithheldOrUnparseable_IsNull(string? handle)
        => Assert.Null(CallScreening.Normalize(handle, 1));


    [Fact]
    public void Decide_Block()
        => Assert.Equal(CallScreeningAction.Block, CallScreening.Decide(new(18005550100, null, CallDirectoryEntryType.Block), false));


    [Fact]
    public void Decide_IdentifyWithLabel()
        => Assert.Equal(CallScreeningAction.Identify, CallScreening.Decide(new(18005550100, "Support"), false));


    [Fact]
    public void Decide_IdentifyWithoutLabel_JustRings()
        => Assert.Equal(CallScreeningAction.Allow, CallScreening.Decide(new(18005550100, " "), false));


    [Fact]
    public void Decide_Unlisted_RingsByDefault()
        => Assert.Equal(CallScreeningAction.Allow, CallScreening.Decide(null, false));


    [Fact]
    public void Decide_Unlisted_BlockedWhenBlockingUnlisted()
        => Assert.Equal(CallScreeningAction.Block, CallScreening.Decide(null, true));


    [Fact]
    public void Decide_Allow_RingsEvenWhenBlockingUnlisted()
        => Assert.Equal(CallScreeningAction.Allow, CallScreening.Decide(new(18005550100, null, CallDirectoryEntryType.Allow), true));


    [Fact]
    public void Decide_Identify_RingsEvenWhenBlockingUnlisted()
        => Assert.Equal(CallScreeningAction.Identify, CallScreening.Decide(new(18005550100, "Support"), true));
}
