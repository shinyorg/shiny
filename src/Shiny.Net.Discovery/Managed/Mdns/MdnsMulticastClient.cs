using System.Net;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Managed.Dns;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed;


/// <summary>
/// Binds the shared multicast socket layer to the mDNS group and speaks DNS messages over it.
/// </summary>
/// <remarks>
/// This is the whole of what used to be MulticastClient that was actually mDNS specific - the
/// sockets, joins and interface fan-out now live in <see cref="MulticastSocketSet"/> so SSDP and
/// WS-Discovery can share them. The surface here is unchanged so the browse session and the
/// publication did not have to move with it.
/// </remarks>
sealed class MdnsMulticastClient : IDisposable
{
    /// <summary>RFC 6762 section 17 - an mDNS message may be up to 9000 bytes.</summary>
    const int MaxPacketSize = 9000;

    static readonly MulticastEndpoint mdnsEndpoint = new(
        Name: "mDNS",
        GroupV4: IPAddress.Parse(MdnsConstants.IPv4MulticastAddress),
        GroupV6: IPAddress.Parse(MdnsConstants.IPv6MulticastAddress),
        Port: MdnsConstants.Port,
        // RFC 6762 section 11 requires 255 so a hop count of one can be verified by the receiver
        Ttl: 255,
        MaxPacketSize: MaxPacketSize,
        // DNS-SD advertises addresses others can connect to, so link-local IPv6 is not useful here
        IncludeIPv6LinkLocal: false,
        // mDNS responses come back to the group, so there is nothing for an ephemeral socket to catch
        UseSearchSocket: false
    );

    readonly MulticastSocketSet sockets;
    readonly ILogger logger;


    public MdnsMulticastClient(ILogger logger)
    {
        this.logger = logger;
        this.sockets = new MulticastSocketSet(mdnsEndpoint, logger);
        this.sockets.DatagramReceived += this.OnDatagramReceived;
    }


    /// <summary>
    /// Raised for every well-formed packet received. Handlers must not throw.
    /// </summary>
    public event Action<DnsMessage, IPEndPoint>? MessageReceived;


    /// <inheritdoc cref="MulticastSocketSet.LocalAddresses"/>
    public IReadOnlyList<IPAddress> LocalAddresses => this.sockets.LocalAddresses;


    /// <inheritdoc cref="MulticastSocketSet.Acquire"/>
    public IDisposable Acquire() => this.sockets.Acquire();


    /// <summary>
    /// Multicasts a message out of every joined interface.
    /// </summary>
    public Task Send(DnsMessage message, CancellationToken ct = default)
        => this.Send(message, null, ct);


    /// <summary>
    /// Sends a message, either to the multicast group (<paramref name="destination"/> null) or
    /// unicast back to a specific peer that asked for a unicast response.
    /// </summary>
    public Task Send(DnsMessage message, IPEndPoint? destination, CancellationToken ct = default)
    {
        var payload = message.ToArray();

        return destination == null
            ? this.sockets.SendMulticast(payload, ct)
            : this.sockets.SendTo(payload, destination, ct);
    }


    void OnDatagramReceived(MulticastDatagram datagram)
    {
        DnsMessage message;
        try
        {
            message = DnsMessage.Parse(datagram.Payload.ToArray());
        }
        catch (DnsFormatException ex)
        {
            this.logger.LogTrace(ex, "Dropped a malformed mDNS packet from {From}", datagram.Source);
            return;
        }

        try
        {
            this.MessageReceived?.Invoke(message, datagram.Source);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An mDNS message handler threw");
        }
    }


    public void Dispose()
    {
        this.sockets.DatagramReceived -= this.OnDatagramReceived;
        this.sockets.Dispose();
    }
}
