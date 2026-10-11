using Shiny.AppStore;
using Xunit;

namespace Shiny.AppStore.Tests;


public class LinuxAppStoreParsingTests
{
    [Fact]
    public void ParseFields_ReadsFlatpakInfo()
    {
        const string output = """

            Calculator - Perform arithmetic, scientific or financial calculations

                      ID: org.gnome.Calculator
                     Ref: app/org.gnome.Calculator/x86_64/stable
                 Version: 48.1
                  Origin: flathub
                    Date: 2026-04-02 10:11:12 +0000
                 Subject: Update to 48.1 (abc123)
            """;

        var fields = LinuxAppStore.ParseFields(output);
        Assert.Equal("org.gnome.Calculator", fields["ID"]);
        Assert.Equal("48.1", fields["Version"]);
        Assert.Equal("flathub", fields["Origin"]);
        Assert.Equal("2026-04-02 10:11:12 +0000", fields["Date"]);
    }


    [Fact]
    public void SnapList_ReadsVersionAndTracking()
    {
        const string output = """
            Name      Version  Rev  Tracking       Publisher   Notes
            myapp     1.2.0    42   latest/stable  someone     -
            """;

        Assert.True(LinuxAppStore.TryParseSnapList(output, out var version, out var tracking));
        Assert.Equal("1.2.0", version);
        Assert.Equal("latest/stable", tracking);
    }


    [Fact]
    public void SnapChannel_FollowsCaretToTheChannelAbove()
    {
        const string info = """
            name:      myapp
            store-url: https://snapcraft.io/myapp
            channels:
              latest/stable:    1.3.0 2026-02-01 (55) 60MB -
              latest/candidate: ^
              latest/beta:      1.4.0-beta 2026-03-01 (60) 61MB -
              latest/edge:      --
            installed:          1.2.0            (42) 60MB -
            """;

        Assert.True(LinuxAppStore.TryParseSnapChannel(info, "latest/candidate", out var version, out var releasedAt));
        Assert.Equal("1.3.0", version);
        Assert.Equal(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero).Date, releasedAt!.Value.Date);

        Assert.True(LinuxAppStore.TryParseSnapChannel(info, "latest/beta", out version, out _));
        Assert.Equal("1.4.0-beta", version);

        Assert.False(LinuxAppStore.TryParseSnapChannel(info, "latest/edge", out _, out _));
        Assert.Equal("https://snapcraft.io/myapp", LinuxAppStore.ParseSnapStoreUrl(info));
    }


    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("v2.0", "2.0")]
    [InlineData("1.4.0-beta.1", "1.4.0")]
    [InlineData("2026", "2026.0")]
    public void TryParseVersion_TakesTheLeadingNumericRun(string input, string expected)
    {
        Assert.True(LinuxAppStore.TryParseVersion(input, out var version));
        Assert.Equal(Version.Parse(expected), version);
    }


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("beta")]
    public void TryParseVersion_FalseWithoutDigits(string? input)
        => Assert.False(LinuxAppStore.TryParseVersion(input, out _));


    [Fact]
    public async Task NotSupportedAppStore_FindsAndOpensNothing()
    {
        var store = new NotSupportedAppStore();
        Assert.Null(await store.GetCurrent());
        Assert.False(await store.OpenStore());
        Assert.False(await store.RequestReview());
    }
}
