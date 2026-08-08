using System.Net;
using Shiny.Net.Discovery.Managed.Multicast;
using Shiny.Net.Discovery.Managed.WsDiscovery;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class LocalNetworkGuardTests
{
    static readonly IPAddress source = IPAddress.Parse("192.168.1.9");


    static bool IsFetchable(string url, string? from = null)
        => LocalNetworkGuard.IsFetchable(new Uri(url), IPAddress.Parse(from ?? source.ToString()), out _);


    [Fact]
    public void AUrlOnTheAdvertisingDeviceIsAllowed()
    {
        Assert.True(IsFetchable("http://192.168.1.9/desc.xml"));
        Assert.True(IsFetchable("http://192.168.1.9:8080/desc.xml"));
        Assert.True(IsFetchable("https://192.168.1.9/desc.xml"));
    }


    [Theory]
    // the core control - a device must not point us at some other host
    [InlineData("http://10.0.0.1/desc.xml", "is not the address")]
    [InlineData("http://169.254.1.1/desc.xml", "is not the address")]
    [InlineData("ftp://192.168.1.9/desc.xml", "is not http or https")]
    [InlineData("file:///etc/passwd", "is not http or https")]
    [InlineData("http://user:pass@192.168.1.9/d.xml", "carries credentials")]
    [InlineData("http://0.0.0.0/desc.xml", "unspecified address")]
    [InlineData("http://example.com/desc.xml", "is a name rather than a literal address")]
    public void UnsafeUrlsAreRefusedWithAReason(string url, string expectedReason)
    {
        Assert.False(LocalNetworkGuard.IsFetchable(new Uri(url), source, out var reason));
        Assert.Contains(expectedReason, reason);
    }


    [Fact]
    public void ARemoteDeviceCannotAdvertiseLoopback()
    {
        Assert.False(LocalNetworkGuard.IsFetchable(new Uri("http://127.0.0.1/desc.xml"), source, out var reason));
        Assert.Contains("loopback", reason);
    }


    [Fact]
    public void ALoopbackDeviceMayAdvertiseLoopback()
        => Assert.True(IsFetchable("http://127.0.0.1/desc.xml", "127.0.0.1"));


    [Fact]
    public void LoopbackAndLinkLocalCountAsLocal()
    {
        Assert.True(LocalNetworkGuard.IsOnLocalNetwork(IPAddress.Loopback));
        Assert.True(LocalNetworkGuard.IsOnLocalNetwork(IPAddress.IPv6Loopback));
        Assert.True(LocalNetworkGuard.IsOnLocalNetwork(IPAddress.Parse("169.254.10.20")));
        Assert.True(LocalNetworkGuard.IsOnLocalNetwork(IPAddress.Parse("fe80::1")));
    }


    [Fact]
    public void APublicAddressIsNotLocal()
        => Assert.False(LocalNetworkGuard.IsOnLocalNetwork(IPAddress.Parse("8.8.8.8")));


    [Fact]
    public void ZoneIdIsAttachedToLinkLocalIPv6()
    {
        var result = LocalNetworkGuard.ApplyZoneId(new Uri("http://[fe80::1]/desc.xml"), 5);

        Assert.Contains("%5", result.DnsSafeHost);
    }


    [Theory]
    [InlineData("http://192.168.1.9/desc.xml")]
    [InlineData("http://[2001:db8::1]/desc.xml")]
    public void ZoneIdIsNotAttachedToAnythingElse(string url)
        => Assert.Equal(new Uri(url), LocalNetworkGuard.ApplyZoneId(new Uri(url), 5));


    [Fact]
    public void ZoneIdIsSkippedWithoutAnInterfaceIndex()
        => Assert.Equal(
            new Uri("http://[fe80::1]/desc.xml"),
            LocalNetworkGuard.ApplyZoneId(new Uri("http://[fe80::1]/desc.xml"), 0)
        );
}


public class WsdTransportTests
{
    static readonly IPAddress source = IPAddress.Parse("192.168.1.9");


    [Fact]
    public void TheAddressMatchingTheSenderWins()
    {
        var addresses = new[]
        {
            new Uri("http://10.9.9.9/onvif"),
            new Uri("http://192.168.1.9/onvif/device_service")
        };

        Assert.Equal(
            "http://192.168.1.9/onvif/device_service",
            WsdTransport.ChoosePreferred(addresses, source, 0)?.AbsoluteUri
        );
    }


    [Fact]
    public void AnUnreachableAdvertisedAddressStillYieldsSomething()
    {
        // a stale address after a DHCP change - there is nothing better, so it is returned
        var addresses = new[] { new Uri("http://10.9.9.9/onvif") };

        Assert.Equal("http://10.9.9.9/onvif", WsdTransport.ChoosePreferred(addresses, source, 0)?.AbsoluteUri);
    }


    [Fact]
    public void NoAddressesYieldsNull()
        => Assert.Null(WsdTransport.ChoosePreferred(Array.Empty<Uri>(), source, 0));


    [Fact]
    public void UnparseableAddressesAreDropped()
    {
        var parsed = WsdTransport.ParseAddresses(new[] { "http://192.168.1.9/a", "not a uri", "  ", "http://192.168.1.9/a" });

        Assert.Equal("http://192.168.1.9/a", Assert.Single(parsed).AbsoluteUri);
    }
}
