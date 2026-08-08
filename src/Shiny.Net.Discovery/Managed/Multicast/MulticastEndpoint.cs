using System.Net;

namespace Shiny.Net.Discovery.Managed.Multicast;


/// <summary>
/// Everything that differs between the multicast protocols this library speaks. One of these
/// is handed to a <see cref="MulticastSocketSet"/> to tell it what group to join and how to
/// behave on it.
/// </summary>
/// <param name="Name">Used in log messages and exceptions, eg "SSDP".</param>
/// <param name="GroupV4">The IPv4 multicast group.</param>
/// <param name="GroupV6">The IPv6 multicast group, or null to run IPv4 only.</param>
/// <param name="Port">The port to bind and send to.</param>
/// <param name="Ttl">
/// The multicast TTL / hop limit. mDNS uses 255 (RFC 6762 requires it), SSDP defaults to 2 and
/// WS-Discovery to 1 - sending mDNS's 255 on those groups leaks traffic past the local link.
/// </param>
/// <param name="MaxPacketSize">The receive buffer size. Datagrams larger than this are truncated.</param>
/// <param name="IncludeIPv6LinkLocal">
/// Whether link-local IPv6 addresses count as usable local addresses. mDNS advertises routable
/// addresses only, but SSDP and WS-Discovery run primarily on the FF02 link-local scope, so they
/// need them.
/// </param>
/// <param name="UseSearchSocket">
/// When true an extra socket is bound to an ephemeral port and used to send queries. Devices are
/// split on where they send unicast replies - some to the source port, some to the well known port
/// regardless - so queries go out of the ephemeral socket while both sockets listen. It also gives
/// Windows a way to receive replies when the system SSDP/WSD services hold the well known port.
/// </param>
sealed record MulticastEndpoint(
    string Name,
    IPAddress GroupV4,
    IPAddress? GroupV6,
    int Port,
    int Ttl,
    int MaxPacketSize,
    bool IncludeIPv6LinkLocal,
    bool UseSearchSocket
);


/// <summary>
/// One received datagram plus the context needed to judge it.
/// </summary>
/// <param name="Payload">
/// The bytes received. Only valid for the duration of the handler - copy anything you keep.
/// </param>
/// <param name="Source">Who sent it. Unicast replies must go back here.</param>
/// <param name="InterfaceIndex">
/// The index of the interface it arrived on, or 0 when the platform did not report one. Needed to
/// attach a zone id to link-local IPv6 URLs and to decide whether a source is on our subnet.
/// </param>
/// <param name="LocalAddress">
/// Our own address on that interface, matched to the source's address family. Null when it could
/// not be determined.
/// </param>
readonly record struct MulticastDatagram(
    ReadOnlyMemory<byte> Payload,
    IPEndPoint Source,
    int InterfaceIndex,
    IPAddress? LocalAddress
);
