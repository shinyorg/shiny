using System.Net;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed.WsDiscovery;


/// <summary>
/// How WS-Discovery sits on the shared multicast socket layer.
/// </summary>
static class WsdTransport
{
    public static readonly MulticastEndpoint Endpoint = new(
        Name: "WS-Discovery",
        GroupV4: IPAddress.Parse(WsDiscoveryConstants.IPv4MulticastAddress),
        GroupV6: IPAddress.Parse(WsDiscoveryConstants.IPv6MulticastAddress),
        Port: WsDiscoveryConstants.Port,
        Ttl: WsDiscoveryConstants.DefaultTtl,
        MaxPacketSize: WsdEnvelopeReader.MaxMessageSize,
        IncludeIPv6LinkLocal: true,
        // ONVIF cameras routinely reply to port 3702 rather than the source port, so both the
        // well known socket and an ephemeral one listen while probes go out of the ephemeral one
        UseSearchSocket: true
    );

    /// <summary>SOAP-over-UDP: a target waits a random slice of this before answering.</summary>
    public static readonly TimeSpan AppMaxDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>SOAP-over-UDP MULTICAST_UDP_REPEAT plus the original transmission.</summary>
    public const int MulticastTransmissions = 3;

    public static readonly TimeSpan UdpMinDelay = TimeSpan.FromMilliseconds(50);
    public static readonly TimeSpan UdpMaxDelay = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan UdpUpperDelay = TimeSpan.FromMilliseconds(500);


    /// <summary>
    /// Picks the transport address most likely to be reachable.
    /// </summary>
    /// <remarks>
    /// Devices advertise addresses that are stale after a DHCP change, that are 0.0.0.0 or
    /// loopback, or that are link-local IPv6 without a zone id. The address the datagram actually
    /// came from is the one piece of ground truth available, so it wins.
    /// </remarks>
    public static Uri? ChoosePreferred(IReadOnlyList<Uri> addresses, IPAddress source, int interfaceIndex)
    {
        if (addresses.Count == 0)
            return null;

        var zoned = addresses
            .Select(x => LocalNetworkGuard.ApplyZoneId(x, interfaceIndex))
            .ToList();

        var fromSource = zoned.FirstOrDefault(x =>
            IPAddress.TryParse(x.DnsSafeHost, out var host) && host.Equals(source)
        );
        if (fromSource != null)
            return fromSource;

        var onLink = zoned.FirstOrDefault(x =>
            IPAddress.TryParse(x.DnsSafeHost, out var host) &&
            !IPAddress.Any.Equals(host) &&
            !IPAddress.IsLoopback(host) &&
            LocalNetworkGuard.IsOnLocalNetwork(host)
        );
        if (onLink != null)
            return onLink;

        var web = zoned.FirstOrDefault(x =>
            x.Scheme == Uri.UriSchemeHttp || x.Scheme == Uri.UriSchemeHttps
        );

        return web ?? zoned[0];
    }


    /// <summary>
    /// Turns the raw XAddrs strings into absolute URIs, dropping anything unusable.
    /// </summary>
    public static IReadOnlyList<Uri> ParseAddresses(IReadOnlyList<string> raw)
        => raw
            .Select(x => Uri.TryCreate(x, UriKind.Absolute, out var uri) ? uri : null)
            .Where(x => x != null)
            .Select(x => x!)
            .Distinct()
            .ToList();
}
