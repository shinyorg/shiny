using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Managed.Dns;

namespace Shiny.Net.Discovery.Managed;


/// <summary>
/// Owns the UDP 5353 sockets, group memberships and send fan-out across every usable interface.
/// One instance is shared by all browse and publish operations.
/// </summary>
sealed class MulticastClient(ILogger logger) : IDisposable
{
    readonly ILogger logger = logger;

    // SO_REUSEPORT is required to co-exist with a system responder (avahi-daemon, mDNSResponder,
    // the Windows DNS client) that already holds 5353. It is not in SocketOptionName and the
    // value differs per platform, so it goes in raw.
    const int SoReusePortLinux = 15;
    const int SoReusePortBsd = 0x0200;

    /// <summary>RFC 6762 section 17 - an mDNS message may be up to 9000 bytes.</summary>
    const int MaxPacketSize = 9000;

    static readonly IPAddress groupV4 = IPAddress.Parse(MdnsConstants.IPv4MulticastAddress);
    static readonly IPAddress groupV6 = IPAddress.Parse(MdnsConstants.IPv6MulticastAddress);
    static readonly IPEndPoint destinationV4 = new(groupV4, MdnsConstants.Port);
    static readonly IPEndPoint destinationV6 = new(groupV6, MdnsConstants.Port);

    readonly SemaphoreSlim sendLock = new(1, 1);
    readonly Lock stateLock = new();
    readonly List<IPAddress> joinedV4 = new();
    readonly List<int> joinedV6 = new();

    Socket? socketV4;
    Socket? socketV6;
    CancellationTokenSource? cancelSource;
    int refCount;


    /// <summary>
    /// Raised for every well-formed packet received. Handlers must not throw.
    /// </summary>
    public event Action<DnsMessage, IPEndPoint>? MessageReceived;


    /// <summary>
    /// The unicast addresses of every interface we successfully joined on. These are what a
    /// publication advertises in its A/AAAA records.
    /// </summary>
    public IReadOnlyList<IPAddress> LocalAddresses
    {
        get
        {
            lock (this.joinedV4)
                return this.joinedV4.Concat(GetLocalV6Addresses()).ToList();
        }
    }


    /// <summary>
    /// Starts the sockets if this is the first lease. Dispose the returned lease to release it -
    /// the sockets close once the last lease goes away.
    /// </summary>
    public IDisposable Acquire()
    {
        lock (this.stateLock)
        {
            if (this.refCount++ == 0)
                this.Start();
        }
        return new Lease(this);
    }


    void Release()
    {
        lock (this.stateLock)
        {
            if (--this.refCount <= 0)
            {
                this.refCount = 0;
                this.Stop();
            }
        }
    }


    void Start()
    {
        this.cancelSource = new CancellationTokenSource();
        var ct = this.cancelSource.Token;

        this.socketV4 = TryCreateSocket(AddressFamily.InterNetwork, this.logger);
        this.socketV6 = Socket.OSSupportsIPv6 ? TryCreateSocket(AddressFamily.InterNetworkV6, this.logger) : null;

        if (this.socketV4 == null && this.socketV6 == null)
            throw new MdnsException($"Could not bind UDP port {MdnsConstants.Port} on any address family. Another process may hold it exclusively.");

        this.JoinGroups();
        NetworkChange.NetworkAddressChanged += this.OnNetworkAddressChanged;

        if (this.socketV4 != null)
            _ = Task.Run(() => this.ReceiveLoop(this.socketV4, AddressFamily.InterNetwork, ct), ct);

        if (this.socketV6 != null)
            _ = Task.Run(() => this.ReceiveLoop(this.socketV6, AddressFamily.InterNetworkV6, ct), ct);
    }


    void Stop()
    {
        NetworkChange.NetworkAddressChanged -= this.OnNetworkAddressChanged;
        this.cancelSource?.Cancel();
        this.cancelSource?.Dispose();
        this.cancelSource = null;

        this.socketV4?.Dispose();
        this.socketV6?.Dispose();
        this.socketV4 = null;
        this.socketV6 = null;

        lock (this.joinedV4)
        {
            this.joinedV4.Clear();
            this.joinedV6.Clear();
        }
    }


    static Socket? TryCreateSocket(AddressFamily family, ILogger logger)
    {
        Socket? socket = null;
        try
        {
            socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            TrySetReusePort(socket);

            var level = family == AddressFamily.InterNetworkV6 ? SocketOptionLevel.IPv6 : SocketOptionLevel.IP;
            if (family == AddressFamily.InterNetworkV6)
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, true);

            socket.Bind(new IPEndPoint(
                family == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any,
                MdnsConstants.Port
            ));

            socket.SetSocketOption(level, SocketOptionName.MulticastTimeToLive, 255);
            socket.SetSocketOption(level, SocketOptionName.MulticastLoopback, true);
            return socket;
        }
        catch (SocketException ex)
        {
            logger.LogWarning(ex, "Could not open the mDNS socket for {Family}", family);
            socket?.Dispose();
            return null;
        }
    }


    static void TrySetReusePort(Socket socket)
    {
        if (OperatingSystem.IsWindows())
            return; // SO_REUSEADDR already gives Windows the sharing semantics we need

        var option = OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()
            ? SoReusePortLinux
            : SoReusePortBsd;

        try
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)option, true);
        }
        catch (Exception)
        {
            // best effort - the platform may not know the option and the bind can still succeed
        }
    }


    void JoinGroups()
    {
        lock (this.joinedV4)
        {
            this.joinedV4.Clear();
            this.joinedV6.Clear();

            foreach (var adapter in GetUsableInterfaces())
            {
                var properties = adapter.GetIPProperties();

                if (this.socketV4 != null)
                {
                    foreach (var unicast in properties.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork &&
                            this.TryJoinV4(unicast.Address))
                        {
                            this.joinedV4.Add(unicast.Address);
                        }
                    }
                }

                if (this.socketV6 != null && this.TryJoinV6(properties, out var index))
                    this.joinedV6.Add(index);
            }

            if (this.joinedV4.Count == 0 && this.joinedV6.Count == 0)
                this.logger.LogWarning("No network interface accepted the mDNS multicast group - discovery will not work until one comes up");
        }
    }


    bool TryJoinV4(IPAddress localAddress)
    {
        try
        {
            this.socketV4!.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                new MulticastOption(groupV4, localAddress)
            );
            return true;
        }
        catch (SocketException)
        {
            // already joined, or the interface went away between enumeration and join
            return false;
        }
    }


    bool TryJoinV6(IPInterfaceProperties properties, out int index)
    {
        index = 0;
        try
        {
            index = properties.GetIPv6Properties().Index;
            this.socketV6!.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.AddMembership,
                new IPv6MulticastOption(groupV6, index)
            );
            return true;
        }
        catch (Exception)
        {
            // GetIPv6Properties throws on adapters without IPv6 configured
            return false;
        }
    }


    void OnNetworkAddressChanged(object? sender, EventArgs args)
    {
        try
        {
            if (this.socketV4 != null || this.socketV6 != null)
            {
                this.logger.LogDebug("Network addresses changed - rejoining the mDNS multicast group");
                this.JoinGroups();
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to rejoin the mDNS multicast group after a network change");
        }
    }


    /// <summary>
    /// Multicasts a message out of every joined interface.
    /// </summary>
    public Task Send(DnsMessage message, CancellationToken ct = default)
        => this.Send(message, null, ct);


    /// <summary>
    /// Sends a message, either to the multicast group (<paramref name="destination"/> null) or
    /// unicast back to a specific peer that asked for a unicast response.
    /// </summary>
    public async Task Send(DnsMessage message, IPEndPoint? destination, CancellationToken ct = default)
    {
        var payload = message.ToArray();

        await this.sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (destination != null)
            {
                var socket = destination.AddressFamily == AddressFamily.InterNetworkV6 ? this.socketV6 : this.socketV4;
                if (socket != null)
                    await SendSafe(socket, payload, destination, ct).ConfigureAwait(false);
            }
            else
            {
                await this.SendMulticast(payload, ct).ConfigureAwait(false);
            }
        }
        finally
        {
            this.sendLock.Release();
        }
    }


    async Task SendMulticast(byte[] payload, CancellationToken ct)
    {
        List<IPAddress> v4;
        List<int> v6;
        lock (this.joinedV4)
        {
            v4 = this.joinedV4.ToList();
            v6 = this.joinedV6.ToList();
        }

        foreach (var local in v4)
        {
            // the group is joined on many interfaces but a datagram only leaves one, so the
            // outbound interface has to be reselected before each send
            if (this.TrySetOutboundV4(local))
                await SendSafe(this.socketV4!, payload, destinationV4, ct).ConfigureAwait(false);
        }

        foreach (var index in v6)
        {
            if (this.TrySetOutboundV6(index))
                await SendSafe(this.socketV6!, payload, destinationV6, ct).ConfigureAwait(false);
        }
    }


    bool TrySetOutboundV4(IPAddress local)
    {
        try
        {
            this.socketV4!.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, local.GetAddressBytes());
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }


    bool TrySetOutboundV6(int index)
    {
        try
        {
            this.socketV6!.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, index);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }


    static async Task SendSafe(Socket socket, byte[] payload, IPEndPoint destination, CancellationToken ct)
    {
        try
        {
            await socket
                .SendToAsync(payload, SocketFlags.None, destination, ct)
                .ConfigureAwait(false);
        }
        catch (SocketException)
        {
            // an interface can drop mid-send - the retransmit timer covers us
        }
        catch (ObjectDisposedException)
        {
            // shutting down
        }
    }


    async Task ReceiveLoop(Socket socket, AddressFamily family, CancellationToken ct)
    {
        var buffer = new byte[MaxPacketSize];
        var source = new IPEndPoint(
            family == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any,
            0
        );

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await socket
                    .ReceiveFromAsync(buffer, SocketFlags.None, source, ct)
                    .ConfigureAwait(false);

                this.Dispatch(buffer.AsSpan(0, result.ReceivedBytes).ToArray(), (IPEndPoint)result.RemoteEndPoint);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException ex)
            {
                this.logger.LogDebug(ex, "mDNS receive error on {Family}", family);
            }
        }
    }


    void Dispatch(byte[] data, IPEndPoint from)
    {
        DnsMessage message;
        try
        {
            message = DnsMessage.Parse(data);
        }
        catch (DnsFormatException ex)
        {
            this.logger.LogTrace(ex, "Dropped a malformed mDNS packet from {From}", from);
            return;
        }

        try
        {
            this.MessageReceived?.Invoke(message, from);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An mDNS message handler threw");
        }
    }


    static IEnumerable<NetworkInterface> GetUsableInterfaces()
    {
        var up = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up && x.SupportsMulticast)
            .ToList();

        var routable = up
            .Where(x => x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToList();

        // fall back to loopback so a dev box with no network still discovers itself
        return routable.Count > 0 ? routable : up;
    }


    static IEnumerable<IPAddress> GetLocalV6Addresses()
        => GetUsableInterfaces()
            .SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .Select(x => x.Address)
            .Where(x => x.AddressFamily == AddressFamily.InterNetworkV6 && !x.IsIPv6LinkLocal);


    public void Dispose()
    {
        lock (this.stateLock)
        {
            this.refCount = 0;
            this.Stop();
        }
        this.sendLock.Dispose();
    }


    sealed class Lease(MulticastClient client) : IDisposable
    {
        int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 0)
                client.Release();
        }
    }
}
