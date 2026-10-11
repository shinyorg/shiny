using System;
using Xunit;

namespace Shiny.Tests;


public class BrowserUserAgentTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.3537.57", "141.0.3537.57")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 OPR/124.0.0.0", "124.0.0.0")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64; rv:143.0) Gecko/20100101 Firefox/143.0", "143.0")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36", "141.0.0.0")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1", "18.6")]
    public void ParsesTheMostSpecificBrowser(string userAgent, string expected)
        => Assert.Equal(Version.Parse(expected), NetPlatform.ParseBrowserVersion(userAgent));


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("curl/8.7.1")]
    public void NullWhenNoBrowserVersion(string? userAgent)
        => Assert.Null(NetPlatform.ParseBrowserVersion(userAgent));


    [Fact]
    public void NoUserAgentOutsideABrowser()
    {
        var platform = new NetPlatform();
        Assert.Null(platform.UserAgent);
        Assert.Null(platform.UserAgentVersion);
    }
}
