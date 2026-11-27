using Shiny.AppStore.Infrastructure;
using Xunit;

namespace Shiny.AppStore.Tests;


public class StoreVersionTests
{
    [Theory]
    [InlineData("2", 2, 0, -1)]
    [InlineData("2.1", 2, 1, -1)]
    [InlineData(" 1.10.3 ", 1, 10, 3)]
    public void Parses_SinglePartAndMultiPart(string value, int major, int minor, int build)
    {
        Assert.True(StoreVersion.TryParse(value, out var version));
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(build, version.Build);
    }


    [Fact]
    public void SinglePart_ComparesWithPaddedAppVersion()
    {
        Assert.True(StoreVersion.TryParse("3", out var store));
        Assert.True(store > new Version(2, 0));
        Assert.False(store > new Version(3, 0));
    }


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("beta")]
    public void Invalid_ReturnsFalse(string? value)
        => Assert.False(StoreVersion.TryParse(value, out _));
}
