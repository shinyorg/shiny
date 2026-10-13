using System;
using System.Globalization;
using Xunit;

namespace Shiny.Tests;


public class PlatformGlobalizationTests
{
    [Fact]
    public void Watch_StartsWithFirstSubscriber_StopsWithLast()
    {
        var starts = 0;
        var stops = 0;
        var monitor = new GlobalizationMonitor(
            this,
            _ => { starts++; return new ActionDisposable(() => stops++); },
            _ => new ActionDisposable(() => { })
        );

        EventHandler<CultureInfo> a = (_, _) => { };
        EventHandler<CultureInfo> b = (_, _) => { };

        monitor.AddCultureHandler(a);
        monitor.AddCultureHandler(b);
        Assert.Equal(1, starts);

        monitor.RemoveCultureHandler(a);
        Assert.Equal(0, stops);

        monitor.RemoveCultureHandler(b);
        Assert.Equal(1, stops);
    }


    [Fact]
    public void CultureChanged_RaisedOnlyForANewCulture()
    {
        var original = CultureInfo.CurrentCulture;
        Action? refresh = null;
        var monitor = new GlobalizationMonitor(
            this,
            r => { refresh = r; return new ActionDisposable(() => { }); },
            _ => new ActionDisposable(() => { })
        );

        CultureInfo? raised = null;
        var count = 0;
        monitor.AddCultureHandler((_, c) => { raised = c; count++; });

        try
        {
            refresh!();
            Assert.Equal(0, count);

            var target = original.Name == "fr-FR" ? new CultureInfo("de-DE") : new CultureInfo("fr-FR");
            CultureInfo.CurrentCulture = target;
            refresh();
            Assert.Equal(1, count);
            Assert.Equal(target, raised);

            refresh();
            Assert.Equal(1, count);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }


    [Theory]
    [InlineData("17.4.1", "17.4.1")]
    [InlineData("14", "14.0")]
    [InlineData("2.3.0-beta (45)", "2.3.0")]
    [InlineData("1.2.3.4", "1.2.3.4")]
    public void PlatformVersionParser_ReadsLeadingVersion(string input, string expected)
        => Assert.Equal(Version.Parse(expected), PlatformVersionParser.Parse(input));


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("beta")]
    public void PlatformVersionParser_NullWhenNoVersion(string? input)
        => Assert.Null(PlatformVersionParser.Parse(input));


    [Fact]
    public void NetPlatform_ReportsTheHost()
    {
        var platform = new NetPlatform();
        Assert.NotNull(platform.AppVersion);
        Assert.NotEqual("Unknown", platform.Platform);
        Assert.Equal(DeviceFormFactor.Desktop, platform.DeviceFormFactor);
        if (!OperatingSystem.IsLinux())
            Assert.Null(platform.ApplicationIdentifier);
        Assert.Equal(TimeZoneInfo.Local, platform.CurrentTimeZone);
    }
}
