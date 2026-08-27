using System.Net;
using System.Net.NetworkInformation;
using Shiny.Net.Wifi.Internals;
using Xunit;

namespace Shiny.Net.Wifi.Tests;


/// <summary>
/// The addressing read has to survive a platform that will not answer for it.
/// </summary>
/// <remarks>
/// GatewayAddresses and DnsAddresses are <c>[UnsupportedOSPlatform("android")]</c> and throw on
/// several of the sandboxed platforms, and the refusal arrives off the property rather than off
/// GetIPProperties - so guarding only the factory call leaves the throw in place. These pin the
/// per-property guard, because the failure it prevents is a caller losing the whole network read
/// (or worse, an exception reaching a UI binding) over a field it never asked for.
/// </remarks>
public class ManagedNetworkInfoTests
{
    [Fact]
    public void PropertiesThatRefuseDoNotFailTheRead()
    {
        var info = ManagedNetworkInfo.Build(new FakeNic("wlan0", new RefusingProperties()));

        Assert.Equal("wlan0", info.InterfaceName);
        Assert.Null(info.Gateway);
        Assert.Null(info.SubnetMask);
        Assert.Empty(info.IpAddresses);
        Assert.Empty(info.DnsAddresses);
    }


    [Fact]
    public void GetIPPropertiesThatRefusesDoesNotFailTheRead()
    {
        var info = ManagedNetworkInfo.Build(new FakeNic("en0", properties: null));

        Assert.Equal("en0", info.InterfaceName);
        Assert.Null(info.Gateway);
        Assert.Empty(info.IpAddresses);
    }


    class FakeNic(string name, IPInterfaceProperties? properties) : NetworkInterface
    {
        public override string Id => name;
        public override string Name => name;
        public override string Description => name;
        public override NetworkInterfaceType NetworkInterfaceType => NetworkInterfaceType.Wireless80211;
        public override OperationalStatus OperationalStatus => OperationalStatus.Up;
        public override long Speed => 0;
        public override bool IsReceiveOnly => false;
        public override bool SupportsMulticast => true;

        // null stands for the platform refusing the factory call itself
        public override IPInterfaceProperties GetIPProperties()
            => properties ?? throw new PlatformNotSupportedException();

        public override PhysicalAddress GetPhysicalAddress() => PhysicalAddress.None;
        public override IPv4InterfaceStatistics GetIPv4Statistics() => throw new PlatformNotSupportedException();
        public override IPInterfaceStatistics GetIPStatistics() => throw new PlatformNotSupportedException();
        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent) => true;
    }


    /// <summary>Hands back the properties object, then refuses every field on it.</summary>
    class RefusingProperties : IPInterfaceProperties
    {
        public override UnicastIPAddressInformationCollection UnicastAddresses => throw new PlatformNotSupportedException();
        public override IPAddressInformationCollection AnycastAddresses => throw new PlatformNotSupportedException();
        public override MulticastIPAddressInformationCollection MulticastAddresses => throw new PlatformNotSupportedException();
        public override IPAddressCollection DnsAddresses => throw new PlatformNotSupportedException();
        public override GatewayIPAddressInformationCollection GatewayAddresses => throw new PlatformNotSupportedException();
        public override IPAddressCollection DhcpServerAddresses => throw new PlatformNotSupportedException();
        public override IPAddressCollection WinsServersAddresses => throw new PlatformNotSupportedException();
        public override bool IsDnsEnabled => false;
        public override bool IsDynamicDnsEnabled => false;
        public override string DnsSuffix => String.Empty;
        public override IPv4InterfaceProperties GetIPv4Properties() => throw new PlatformNotSupportedException();
        public override IPv6InterfaceProperties GetIPv6Properties() => throw new PlatformNotSupportedException();
    }
}
