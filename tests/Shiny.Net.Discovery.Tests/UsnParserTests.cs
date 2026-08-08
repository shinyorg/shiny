using Shiny.Net.Discovery.Managed.Ssdp;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class UsnParserTests
{
    [Theory]
    [InlineData("uuid:abc::upnp:rootdevice", "uuid:abc", "upnp:rootdevice")]
    [InlineData("uuid:abc", "uuid:abc", null)]
    [InlineData("uuid:abc::urn:schemas-upnp-org:device:MediaServer:1", "uuid:abc", "urn:schemas-upnp-org:device:MediaServer:1")]
    [InlineData("uuid:abc::urn:schemas-upnp-org:service:ContentDirectory:1", "uuid:abc", "urn:schemas-upnp-org:service:ContentDirectory:1")]
    public void ParseSplitsOnTheFirstDoubleColon(string usn, string expectedUdn, string? expectedType)
    {
        Assert.True(UsnParser.TryParse(usn, out var parts));
        Assert.Equal(expectedUdn, parts.Udn);
        Assert.Equal(expectedType, parts.NotificationType);
    }


    [Theory]
    [InlineData("uuid:2f402f80-da50-11e1-9b23-0017880924f0", "uuid:2f402f80-da50-11e1-9b23-0017880924f0")]
    [InlineData("2f402f80-da50-11e1-9b23-0017880924f0", "uuid:2f402f80-da50-11e1-9b23-0017880924f0")]
    [InlineData("uuid:{2f402f80-da50-11e1-9b23-0017880924f0}", "uuid:2f402f80-da50-11e1-9b23-0017880924f0")]
    [InlineData("  uuid:abc  ", "uuid:abc")]
    [InlineData("UUID:ABC", "uuid:ABC")]
    public void NormalizeUdnHandlesRealWorldSpellings(string raw, string expected)
        => Assert.Equal(expected, UsnParser.NormalizeUdn(raw));


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseRejectsAnEmptyUsn(string? usn)
        => Assert.False(UsnParser.TryParse(usn, out _));


    [Fact]
    public void TypeUrnIsBrokenIntoItsParts()
    {
        Assert.True(UsnParser.TryParseTypeUrn("urn:schemas-upnp-org:device:MediaServer:2", out var urn));

        Assert.Equal("schemas-upnp-org", urn.Domain);
        Assert.Equal("device", urn.Kind);
        Assert.Equal("MediaServer", urn.Type);
        Assert.Equal(2, urn.Version);
        Assert.True(urn.IsDevice);
        Assert.False(urn.IsService);
        Assert.Equal("urn:schemas-upnp-org:device:MediaServer", urn.BaseUrn);
    }


    [Fact]
    public void VendorTypeUrnsAreAccepted()
    {
        Assert.True(UsnParser.TryParseTypeUrn("urn:my-vendor-com:service:Thing:1", out var urn));

        Assert.Equal("my-vendor-com", urn.Domain);
        Assert.True(urn.IsService);
    }


    [Theory]
    [InlineData("upnp:rootdevice")]
    [InlineData("uuid:abc")]
    [InlineData("ssdp:all")]
    [InlineData("urn:schemas-upnp-org:device:MediaServer")]   // no version
    [InlineData("urn:schemas-upnp-org:thing:MediaServer:1")]  // not device or service
    [InlineData("")]
    public void NonTypeUrnsAreRejected(string value)
        => Assert.False(UsnParser.TryParseTypeUrn(value, out _));
}


public class SsdpMatcherTests
{
    [Theory]
    [InlineData("ssdp:all", "upnp:rootdevice")]
    [InlineData("ssdp:all", "uuid:abc")]
    [InlineData("upnp:rootdevice", "upnp:rootdevice")]
    [InlineData("UPNP:ROOTDEVICE", "upnp:rootdevice")]
    [InlineData("uuid:abc", "uuid:abc")]
    [InlineData("uuid:{abc}", "uuid:abc")]
    public void ExactAndWildcardTargetsMatch(string searchTarget, string advertised)
        => Assert.True(SsdpMatcher.Matches(searchTarget, advertised));


    [Theory]
    [InlineData("upnp:rootdevice", "uuid:abc")]
    [InlineData("uuid:abc", "uuid:def")]
    [InlineData("urn:schemas-upnp-org:device:MediaServer:1", "urn:schemas-upnp-org:device:Printer:1")]
    public void UnrelatedTargetsDoNotMatch(string searchTarget, string advertised)
        => Assert.False(SsdpMatcher.Matches(searchTarget, advertised));


    [Fact]
    public void ADeviceAnswersSearchesForItsVersionOrLower()
    {
        // UDA 1.3.2 - a version 2 device answers a version 1 search
        Assert.True(SsdpMatcher.Matches("urn:schemas-upnp-org:device:MediaServer:1", "urn:schemas-upnp-org:device:MediaServer:2"));
        Assert.True(SsdpMatcher.Matches("urn:schemas-upnp-org:device:MediaServer:2", "urn:schemas-upnp-org:device:MediaServer:2"));
    }


    [Fact]
    public void ADeviceDoesNotAnswerSearchesForAHigherVersion()
        => Assert.False(SsdpMatcher.Matches("urn:schemas-upnp-org:device:MediaServer:3", "urn:schemas-upnp-org:device:MediaServer:2"));


    [Fact]
    public void DeviceAndServiceOfTheSameNameAreDistinct()
        => Assert.False(SsdpMatcher.Matches("urn:schemas-upnp-org:device:Thing:1", "urn:schemas-upnp-org:service:Thing:1"));


    [Theory]
    [InlineData(null, "upnp:rootdevice")]
    [InlineData("ssdp:all", null)]
    [InlineData("", "")]
    public void MissingValuesNeverMatch(string? searchTarget, string? advertised)
        => Assert.False(SsdpMatcher.Matches(searchTarget, advertised));


    [Theory]
    [InlineData("uuid:abc", "upnp:rootdevice", "uuid:abc::upnp:rootdevice")]
    [InlineData("uuid:abc", "urn:schemas-upnp-org:device:X:1", "uuid:abc::urn:schemas-upnp-org:device:X:1")]
    // the bare UDN advertisement carries no "::" suffix
    [InlineData("uuid:abc", "uuid:abc", "uuid:abc")]
    [InlineData("uuid:abc", null, "uuid:abc")]
    public void UsnIsBuiltFromTheUdnAndType(string udn, string? notificationType, string expected)
        => Assert.Equal(expected, SsdpMatcher.BuildUsn(udn, notificationType));
}
