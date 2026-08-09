using System.Text;
using Shiny.Net.Discovery.Managed.Ssdp;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class SsdpMessageTests
{
    static SsdpMessage Parse(string text)
    {
        Assert.True(SsdpMessage.TryParse(Encoding.UTF8.GetBytes(text), out var message));
        return message!;
    }


    [Fact]
    public void SearchIsClassifiedAndHeadersRead()
    {
        var message = Parse(
            "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: 239.255.255.250:1900\r\n" +
            "MAN: \"ssdp:discover\"\r\n" +
            "MX: 3\r\n" +
            "ST: ssdp:all\r\n" +
            "\r\n"
        );

        Assert.Equal(SsdpMessageKind.Search, message.Kind);
        Assert.Equal("ssdp:all", message["ST"]);
        Assert.Equal("3", message["MX"]);
    }


    [Fact]
    public void NotifyIsClassified()
    {
        var message = Parse("NOTIFY * HTTP/1.1\r\nNTS: ssdp:alive\r\n\r\n");

        Assert.Equal(SsdpMessageKind.Notify, message.Kind);
        Assert.Equal("ssdp:alive", message["NTS"]);
    }


    [Fact]
    public void ResponseIsClassified()
    {
        var message = Parse("HTTP/1.1 200 OK\r\nST: upnp:rootdevice\r\n\r\n");

        Assert.Equal(SsdpMessageKind.Response, message.Kind);
    }


    [Fact]
    public void HeaderNamesAreCaseInsensitive()
    {
        var message = Parse("NOTIFY * HTTP/1.1\r\nLocation: http://192.168.1.5/d.xml\r\n\r\n");

        Assert.Equal("http://192.168.1.5/d.xml", message["LOCATION"]);
        Assert.Equal("http://192.168.1.5/d.xml", message["location"]);
    }


    [Fact]
    public void BareLineFeedsAreAccepted()
    {
        var message = Parse("NOTIFY * HTTP/1.1\nNT: upnp:rootdevice\nUSN: uuid:abc\n\n");

        Assert.Equal("upnp:rootdevice", message["NT"]);
        Assert.Equal("uuid:abc", message["USN"]);
    }


    [Fact]
    public void EmptyExtValueIsNotAnError()
    {
        var message = Parse("HTTP/1.1 200 OK\r\nEXT:\r\nST: ssdp:all\r\n\r\n");

        Assert.Equal(String.Empty, message["EXT"]);
        Assert.Equal("ssdp:all", message["ST"]);
    }


    [Fact]
    public void FirstValueOfADuplicatedHeaderWins()
    {
        var message = Parse("NOTIFY * HTTP/1.1\r\nUSN: uuid:first\r\nUSN: uuid:second\r\n\r\n");

        Assert.Equal("uuid:first", message["USN"]);
    }


    [Fact]
    public void TrailingNulPaddingIsIgnored()
    {
        var bytes = Encoding.UTF8.GetBytes("NOTIFY * HTTP/1.1\r\nNT: upnp:rootdevice\r\n\r\n\0\0\0");

        Assert.True(SsdpMessage.TryParse(bytes, out var message));
        Assert.Equal("upnp:rootdevice", message!["NT"]);
    }


    [Fact]
    public void ContentAfterTheBlankLineIsIgnored()
    {
        // a few devices pack two messages into one datagram
        var message = Parse("NOTIFY * HTTP/1.1\r\nNT: a\r\n\r\nNOTIFY * HTTP/1.1\r\nNT: b\r\n\r\n");

        Assert.Equal("a", message["NT"]);
    }


    [Theory]
    [InlineData("")]
    [InlineData("GARBAGE")]
    [InlineData("GET / HTTP/1.1\r\n\r\n")]
    public void NonSsdpDatagramsAreRejected(string text)
        => Assert.False(SsdpMessage.TryParse(Encoding.UTF8.GetBytes(text), out _));


    [Fact]
    public void OversizedDatagramsAreRejected()
        => Assert.False(SsdpMessage.TryParse(new byte[SsdpMessage.MaxMessageSize + 1], out _));


    [Theory]
    [InlineData("max-age=1800", 1800)]
    [InlineData("max-age = 1800", 1800)]
    [InlineData("Max-Age=1800", 1800)]
    [InlineData("max-age=1800, must-revalidate", 1800)]
    [InlineData("no-cache", 1800)]                 // falls back
    [InlineData("max-age=1", 60)]                  // clamped up
    [InlineData("max-age=999999999", 86400)]       // clamped down
    public void MaxAgeIsParsedTolerantlyAndClamped(string header, int expectedSeconds)
    {
        var message = Parse($"NOTIFY * HTTP/1.1\r\nCACHE-CONTROL: {header}\r\n\r\n");

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), message.GetMaxAge(TimeSpan.FromMinutes(30)));
    }


    [Fact]
    public void BootIdIsParsedAndOutOfRangeValuesRejected()
    {
        var message = Parse("NOTIFY * HTTP/1.1\r\nBOOTID.UPNP.ORG: 42\r\nCONFIGID.UPNP.ORG: 4294967295\r\n\r\n");

        Assert.Equal(42u, message.GetUInt("BOOTID.UPNP.ORG"));
        // BOOTID/CONFIGID are 31-bit, so anything above int.MaxValue is not usable
        Assert.Null(message.GetUInt("CONFIGID.UPNP.ORG"));
    }


    [Fact]
    public void SearchRoundTrips()
    {
        var bytes = SsdpMessageBuilder.Search("upnp:rootdevice", 3, "239.255.255.250:1900", "Test CP");
        var message = Parse(Encoding.UTF8.GetString(bytes));

        Assert.Equal(SsdpMessageKind.Search, message.Kind);
        Assert.Equal("\"ssdp:discover\"", message["MAN"]);
        Assert.Equal("upnp:rootdevice", message["ST"]);
        Assert.Equal("Test CP", message["CPFN.UPNP.ORG"]);
    }


    [Fact]
    public void AliveRoundTrips()
    {
        var bytes = SsdpMessageBuilder.Alive(
            "239.255.255.250:1900",
            "upnp:rootdevice",
            "uuid:abc::upnp:rootdevice",
            new Uri("http://192.168.1.5:8080/desc.xml"),
            TimeSpan.FromMinutes(30),
            7,
            1,
            null
        );
        var message = Parse(Encoding.UTF8.GetString(bytes));

        Assert.Equal(SsdpMessageKind.Notify, message.Kind);
        Assert.Equal("ssdp:alive", message["NTS"]);
        Assert.Equal("http://192.168.1.5:8080/desc.xml", message["LOCATION"]);
        Assert.Equal(7u, message.GetUInt("BOOTID.UPNP.ORG"));
        Assert.Equal(TimeSpan.FromMinutes(30), message.GetMaxAge(TimeSpan.Zero));
    }


    [Fact]
    public void ByeByeCarriesNoLocationOrCacheControl()
    {
        var bytes = SsdpMessageBuilder.ByeBye("239.255.255.250:1900", "upnp:rootdevice", "uuid:abc::upnp:rootdevice", 7, 1);
        var message = Parse(Encoding.UTF8.GetString(bytes));

        Assert.Equal("ssdp:byebye", message["NTS"]);
        Assert.Null(message["LOCATION"]);
        Assert.Null(message["CACHE-CONTROL"]);
    }
}
