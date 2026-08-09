using System.Net;
using System.Net.Sockets;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed.Ssdp;


/// <summary>
/// How SSDP sits on the shared multicast socket layer.
/// </summary>
static class SsdpTransport
{
    public static readonly MulticastEndpoint Endpoint = new(
        Name: "SSDP",
        GroupV4: IPAddress.Parse(SsdpConstants.IPv4MulticastAddress),
        GroupV6: IPAddress.Parse(SsdpConstants.IPv6MulticastAddress),
        Port: SsdpConstants.Port,
        Ttl: SsdpConstants.DefaultTtl,
        MaxPacketSize: SsdpMessage.MaxMessageSize,
        // SSDP runs on the FF02 link-local scope, so link-local addresses are the normal case
        IncludeIPv6LinkLocal: true,
        // devices are split on whether they reply to the source port or to 1900, and on Windows
        // the system SSDP service holds 1900 - an ephemeral socket catches what 1900 may not
        UseSearchSocket: true
    );

    static readonly string hostV4 = $"{SsdpConstants.IPv4MulticastAddress}:{SsdpConstants.Port}";
    static readonly string hostV6 = $"[{SsdpConstants.IPv6MulticastAddress}]:{SsdpConstants.Port}";


    /// <summary>
    /// The HOST header value for a family. It names the group being sent to, so the IPv4 and
    /// IPv6 copies of a datagram are not byte-identical.
    /// </summary>
    public static string HostHeader(AddressFamily family)
        => family == AddressFamily.InterNetworkV6 ? hostV6 : hostV4;
}
