using System.Net;
using System.Net.Sockets;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class MdnsServiceTests
{
    static MdnsService Build(params IPAddress[] addresses) => new()
    {
        InstanceName = "Living Room",
        ServiceType = "_http._tcp",
        HostName = "mybox.local",
        Port = 8080,
        Addresses = addresses,
        TxtRecords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["version"] = "3",
            ["path"] = "/api",
            ["secure"] = "true"
        }
    };


    [Fact]
    public void FullNameJoinsInstanceTypeAndDomain()
        => Assert.Equal("Living Room._http._tcp.local", Build().FullName);


    [Fact]
    public void IsResolvedNeedsBothAPortAndAnAddress()
    {
        Assert.True(Build(IPAddress.Loopback).IsResolved);
        Assert.False(Build().IsResolved);
        Assert.False((Build(IPAddress.Loopback) with { Port = 0 }).IsResolved);
    }


    [Fact]
    public void GetEndPointFiltersByAddressFamily()
    {
        var service = Build(IPAddress.Parse("192.168.1.5"), IPAddress.Parse("fe80::1"));

        Assert.Equal(new IPEndPoint(IPAddress.Parse("192.168.1.5"), 8080), service.GetEndPoint());
        Assert.Equal(new IPEndPoint(IPAddress.Parse("fe80::1"), 8080), service.GetEndPoint(AddressFamily.InterNetworkV6));
        Assert.Null(Build().GetEndPoint());
    }


    [Fact]
    public void GetEndPointReturnsNullWhenUnresolved()
        => Assert.Null((Build(IPAddress.Loopback) with { Port = 0 }).GetEndPoint());


    [Fact]
    public void GetTxtReadsAndParses()
    {
        var service = Build();

        Assert.Equal("/api", service.GetTxt("path"));
        Assert.Equal("/api", service.GetTxt("PATH"));
        Assert.Null(service.GetTxt("missing"));

        Assert.Equal(3, service.GetTxt<int>("version"));
        Assert.True(service.GetTxt<bool>("secure"));
    }


    [Fact]
    public void GetTxtFallsBackWhenAbsentOrUnparsable()
    {
        var service = Build();

        Assert.Equal(99, service.GetTxt("missing", 99));
        Assert.Equal(7, service.GetTxt("path", 7));   // "/api" is not an int
    }
}
