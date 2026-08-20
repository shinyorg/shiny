using System.Net;
using Xunit;

namespace Shiny.Net.Wifi.Tests;


public class WifiNetworkInfoTests
{
    static WifiNetworkInfo Build(params string[] addresses) => new()
    {
        InterfaceName = "wlan0",
        Ssid = "Kitchen",
        Bssid = "aa:bb:cc:dd:ee:ff",
        IpAddresses = addresses.Select(IPAddress.Parse).ToArray(),
        DnsAddresses = new[] { IPAddress.Parse("1.1.1.1") },
        Gateway = IPAddress.Parse("192.168.1.1"),
        FrequencyMhz = 5180
    };


    [Fact]
    public void EqualityComparesAddressesByValue()
    {
        // the record-synthesized Equals would compare the two arrays by reference, which makes
        // every poll look like a change - this type exists to be diffed
        Assert.Equal(Build("192.168.1.50"), Build("192.168.1.50"));
        Assert.Equal(Build("192.168.1.50").GetHashCode(), Build("192.168.1.50").GetHashCode());
    }


    [Fact]
    public void ADifferentLeaseIsADifferentNetwork()
        => Assert.NotEqual(Build("192.168.1.50"), Build("192.168.1.51"));


    [Fact]
    public void ADifferentDnsSetIsADifferentNetwork()
    {
        var a = Build("192.168.1.50");
        var b = a with { DnsAddresses = new[] { IPAddress.Parse("8.8.8.8") } };
        Assert.NotEqual(a, b);
    }


    [Fact]
    public void AddressOrderIsSignificant()
        => Assert.NotEqual(
            Build("192.168.1.50", "192.168.1.60"),
            Build("192.168.1.60", "192.168.1.50")
        );


    [Fact]
    public void BandAndChannelComeFromTheFrequency()
    {
        var info = Build("192.168.1.50");
        Assert.Equal(WifiBand.FiveGhz, info.Band);
        Assert.Equal(36, info.Channel);
    }


    [Fact]
    public void AddressFamiliesAreSplitOut()
    {
        var info = Build("192.168.1.50", "fe80::1");
        Assert.Equal(IPAddress.Parse("192.168.1.50"), info.IPv4Address);
        Assert.Equal(IPAddress.Parse("fe80::1"), info.IPv6Address);
    }


    [Fact]
    public void MissingAddressFamiliesAreNull()
    {
        var info = Build();
        Assert.Null(info.IPv4Address);
        Assert.Null(info.IPv6Address);
    }
}
