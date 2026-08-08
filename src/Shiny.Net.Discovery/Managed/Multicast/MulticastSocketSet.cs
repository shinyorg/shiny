using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Discovery.Managed.Multicast;


/// <summary>
/// Owns the UDP sockets, group memberships and send fan-out across every usable interface for one
/// multicast protocol. One instance is shared by every operation on that protocol.
/// </summary>
/// <remarks>
/// This started life as the mDNS responder's socket layer and is now driven by a
/// <see cref="MulticastEndpoint"/> so SSDP and WS-Discovery can reuse the awkward parts - the
/// per-interface joins, the outbound interface reselection before every send, and the rejoin on
/// network change.
/// </remarks>
sealed class MulticastSocketSet(MulticastEndpoint endpoint, ILogger logger) : IDisposable
{
    // SO_REUSEPORT is required to co-exist with a system service (avahi-daemon, mDNSResponder,
    // Windows' SSDP Discovery and Function Discovery) that already holds the port. It is not in
    // SocketOptionName and the value differs per platform, so it goes in raw.
    const int SoReusePortLinux = 15;
    const int SoReusePortBsd = 0x0200;

    readonly SemaphoreSlim sendLock = new(1, 1);
    readonly Lock stateLock = new();
    readonly Lock joinLock = new();
    readonly List<V4Membership> joinedV4 = new();
    readonly List<int> joinedV6 = new();
    readonly Dictionary<int, List<IPAddress>> interfaceAddresses = new();

    readonly IPEndPoint destinationV4 = new(endpoint.GroupV4, endpoint.Port);
    readonly IPEndPoint? destinationV6 = endpoint.GroupV6 == null ? null : new IPEndPoint(endpoint.GroupV6, endpoint.Port);

    Socket? socketV4;
    Socket? socketV6;
    Socket? searchV4;
    Socket? searchV6;
    MulticastLockScope? multicastLock;
    CancellationTokenSource? cancelSource;
    int refCount;


    /// <summary>
    /// Raised for every datagram received on any of this protocol's sockets. Handlers must not throw.
    /// </summary>
    public event Action<MulticastDatagram>? DatagramReceived;


    public MulticastEndpoint Endpoint => endpoint;


    /// <summary>
    /// The unicast addresses of every interface we successfully joined on. These are what a
    /// publication advertises as its own reachable addresses.
    /// </summary>
    public IReadOnlyList<IPAddress> LocalAddresses
    {
        get
        {
            lock (this.joinLock)
                return this.joinedV4.Select(x => x.LocalAddress).Concat(this.GetLocalV6Addresses()).ToList();
        }
    }


    /// <summary>
    /// True when the address belongs to this host. Used to drop our own datagrams, which come
    /// back through multicast loopback.
    /// </summary>
    public bool IsLocalAddress(IPAddress address)
    {
        lock (this.joinLock)
        {
            foreach (var addresses in this.interfaceAddresses.Values)
            {
                if (addresses.Contains(address))
                    return true;
            }
        }
        return IPAddress.IsLoopback(address);
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
            {
                try
                {
                    this.Start();
                }
                catch
                {
                    this.refCount = 0;
                    this.Stop();
                    throw;
                }
            }
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

        // the lock must be held before the first receive or Android's Wi-Fi driver filters
        // everything out and the sockets look like they are on a dead network
        this.multicastLock = MulticastLockScope.Acquire(logger, $"shiny-{endpoint.Name.ToLowerInvariant()}");

        this.socketV4 = this.TryCreateSocket(AddressFamily.InterNetwork, endpoint.Port);
        this.socketV6 = endpoint.GroupV6 != null && Socket.OSSupportsIPv6
            ? this.TryCreateSocket(AddressFamily.InterNetworkV6, endpoint.Port)
            : null;

        if (this.socketV4 == null && this.socketV6 == null)
            throw new DiscoveryException($"Could not bind UDP port {endpoint.Port} for {endpoint.Name} on any address family. Another process may hold it exclusively.");

        if (endpoint.UseSearchSocket)
        {
            // port 0 - the OS picks. Failure here is survivable; queries fall back to the group socket
            this.searchV4 = this.socketV4 == null ? null : this.TryCreateSocket(AddressFamily.InterNetwork, 0);
            this.searchV6 = this.socketV6 == null ? null : this.TryCreateSocket(AddressFamily.InterNetworkV6, 0);
        }

        this.JoinGroups();
        NetworkChange.NetworkAddressChanged += this.OnNetworkAddressChanged;

        this.StartReceiveLoop(this.socketV4, AddressFamily.InterNetwork, ct);
        this.StartReceiveLoop(this.socketV6, AddressFamily.InterNetworkV6, ct);
        this.StartReceiveLoop(this.searchV4, AddressFamily.InterNetwork, ct);
        this.StartReceiveLoop(this.searchV6, AddressFamily.InterNetworkV6, ct);
    }


    void StartReceiveLoop(Socket? socket, AddressFamily family, CancellationToken ct)
    {
        if (socket != null)
            _ = Task.Run(() => this.ReceiveLoop(socket, family, ct), ct);
    }


    void Stop()
    {
        NetworkChange.NetworkAddressChanged -= this.OnNetworkAddressChanged;
        this.cancelSource?.Cancel();
        this.cancelSource?.Dispose();
        this.cancelSource = null;

        this.socketV4?.Dispose();
        this.socketV6?.Dispose();
        this.searchV4?.Dispose();
        this.searchV6?.Dispose();
        this.socketV4 = null;
        this.socketV6 = null;
        this.searchV4 = null;
        this.searchV6 = null;

        this.multicastLock?.Dispose();
        this.multicastLock = null;

        lock (this.joinLock)
        {
            this.joinedV4.Clear();
            this.joinedV6.Clear();
            this.interfaceAddresses.Clear();
        }
    }


    Socket? TryCreateSocket(AddressFamily family, int port)
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
                port
            ));

            socket.SetSocketOption(level, SocketOptionName.MulticastTimeToLive, endpoint.Ttl);
            socket.SetSocketOption(level, SocketOptionName.MulticastLoopback, true);
            TrySetPacketInformation(socket, level);
            return socket;
        }
        catch (SocketException ex)
        {
            var permission = PermissionDiagnostics.Translate(ex, endpoint.Name);
            if (permission != null)
            {
                socket?.Dispose();
                throw permission;
            }

            logger.LogWarning(ex, "Could not open the {Protocol} socket for {Family} on port {Port}", endpoint.Name, family, port);
            socket?.Dispose();
            return null;
        }
    }


    /// <summary>
    /// Asks the OS to report which interface each datagram arrived on. Without it we cannot attach
    /// a zone id to link-local IPv6 URLs or tell whether a sender is on our own subnet.
    /// </summary>
    static void TrySetPacketInformation(Socket socket, SocketOptionLevel level)
    {
        try
        {
            socket.SetSocketOption(level, SocketOptionName.PacketInformation, true);
        }
        catch (Exception)
        {
            // best effort - we fall back to an interface index of 0
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
        lock (this.joinLock)
        {
            this.joinedV4.Clear();
            this.joinedV6.Clear();
            this.interfaceAddresses.Clear();

            foreach (var adapter in GetUsableInterfaces())
            {
                var properties = adapter.GetIPProperties();
                var index = TryGetInterfaceIndex(properties);

                var addresses = properties.UnicastAddresses
                    .Select(x => x.Address)
                    .ToList();

                if (index != 0)
                    this.interfaceAddresses[index] = addresses;

                if (this.socketV4 != null)
                {
                    foreach (var address in addresses)
                    {
                        if (address.AddressFamily == AddressFamily.InterNetwork && this.TryJoinV4(address))
                            this.joinedV4.Add(new V4Membership(address, index));
                    }
                }

                if (this.socketV6 != null && index != 0 && this.TryJoinV6(index))
                    this.joinedV6.Add(index);
            }

            if (this.joinedV4.Count == 0 && this.joinedV6.Count == 0)
                logger.LogWarning("No network interface accepted the {Protocol} multicast group - discovery will not work until one comes up", endpoint.Name);
        }
    }


    static int TryGetInterfaceIndex(IPInterfaceProperties properties)
    {
        try
        {
            return properties.GetIPv4Properties().Index;
        }
        catch (Exception)
        {
            // adapters without IPv4 configured throw
        }

        try
        {
            return properties.GetIPv6Properties().Index;
        }
        catch (Exception)
        {
            return 0;
        }
    }


    bool TryJoinV4(IPAddress localAddress)
    {
        try
        {
            this.socketV4!.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                new MulticastOption(endpoint.GroupV4, localAddress)
            );
            return true;
        }
        catch (SocketException)
        {
            // already joined, or the interface went away between enumeration and join
            return false;
        }
    }


    bool TryJoinV6(int index)
    {
        try
        {
            this.socketV6!.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.AddMembership,
                new IPv6MulticastOption(endpoint.GroupV6!, index)
            );
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


    void OnNetworkAddressChanged(object? sender, EventArgs args)
    {
        try
        {
            if (this.socketV4 != null || this.socketV6 != null)
            {
                logger.LogDebug("Network addresses changed - rejoining the {Protocol} multicast group", endpoint.Name);
                this.JoinGroups();
                this.NetworkChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to rejoin the {Protocol} multicast group after a network change", endpoint.Name);
        }
    }


    /// <summary>
    /// Raised after the group was rejoined because addresses changed. Publications use it to bump
    /// their boot id and re-announce.
    /// </summary>
    public event Action? NetworkChanged;


    /// <summary>
    /// Multicasts a payload out of every joined interface, from the well known port.
    /// </summary>
    public Task SendMulticast(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
        => this.SendMulticastCore(_ => payload, this.socketV4, this.socketV6, ct);


    /// <summary>
    /// Multicasts a payload whose bytes depend on the address family. SSDP's HOST header names
    /// the group being sent to, so the v4 and v6 datagrams are not identical.
    /// </summary>
    public Task SendMulticast(Func<AddressFamily, ReadOnlyMemory<byte>> payload, CancellationToken ct = default)
        => this.SendMulticastCore(payload, this.socketV4, this.socketV6, ct);


    /// <summary>
    /// Multicasts a query. Goes out of the ephemeral search socket when the protocol uses one, so
    /// devices that reply to the source port reach us on a port the OS is not sharing with a
    /// system service.
    /// </summary>
    public Task SendQuery(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
        => this.SendQuery(_ => payload, ct);


    /// <inheritdoc cref="SendQuery(ReadOnlyMemory{byte}, CancellationToken)"/>
    public Task SendQuery(Func<AddressFamily, ReadOnlyMemory<byte>> payload, CancellationToken ct = default)
        => this.SendMulticastCore(
            payload,
            this.searchV4 ?? this.socketV4,
            this.searchV6 ?? this.socketV6,
            ct
        );


    /// <summary>
    /// Sends a payload unicast to a specific peer, from the well known port.
    /// </summary>
    public async Task SendTo(ReadOnlyMemory<byte> payload, IPEndPoint destination, CancellationToken ct = default)
    {
        var socket = destination.AddressFamily == AddressFamily.InterNetworkV6 ? this.socketV6 : this.socketV4;
        if (socket == null)
            return;

        await this.sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await this.SendSafe(socket, payload, destination, ct).ConfigureAwait(false);
        }
        finally
        {
            this.sendLock.Release();
        }
    }


    async Task SendMulticastCore(
        Func<AddressFamily, ReadOnlyMemory<byte>> payload,
        Socket? v4Socket,
        Socket? v6Socket,
        CancellationToken ct
    )
    {
        List<V4Membership> v4;
        List<int> v6;
        lock (this.joinLock)
        {
            v4 = this.joinedV4.ToList();
            v6 = this.joinedV6.ToList();
        }

        await this.sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (v4Socket != null && v4.Count > 0)
            {
                var v4Payload = payload(AddressFamily.InterNetwork);
                foreach (var membership in v4)
                {
                    // the group is joined on many interfaces but a datagram only leaves one, so the
                    // outbound interface has to be reselected before each send
                    if (TrySetOutboundV4(v4Socket, membership.LocalAddress))
                        await this.SendSafe(v4Socket, v4Payload, this.destinationV4, ct).ConfigureAwait(false);
                }
            }

            if (v6Socket != null && this.destinationV6 != null && v6.Count > 0)
            {
                var v6Payload = payload(AddressFamily.InterNetworkV6);
                foreach (var index in v6)
                {
                    if (TrySetOutboundV6(v6Socket, index))
                        await this.SendSafe(v6Socket, v6Payload, this.destinationV6, ct).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            this.sendLock.Release();
        }
    }


    static bool TrySetOutboundV4(Socket socket, IPAddress local)
    {
        try
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, local.GetAddressBytes());
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }


    static bool TrySetOutboundV6(Socket socket, int index)
    {
        try
        {
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, index);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }


    async Task SendSafe(Socket socket, ReadOnlyMemory<byte> payload, IPEndPoint destination, CancellationToken ct)
    {
        try
        {
            await socket
                .SendToAsync(payload, SocketFlags.None, destination, ct)
                .ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            // an interface can drop mid-send - the retransmit timer covers us. A permission denial
            // is not transient though, and silently swallowing it is how "no devices found" happens
            var permission = PermissionDiagnostics.Translate(ex, endpoint.Name);
            if (permission != null)
                throw permission;

            logger.LogTrace(ex, "{Protocol} send to {Destination} failed", endpoint.Name, destination);
        }
        catch (ObjectDisposedException)
        {
            // shutting down
        }
    }


    async Task ReceiveLoop(Socket socket, AddressFamily family, CancellationToken ct)
    {
        var buffer = new byte[endpoint.MaxPacketSize];
        var anyEndPoint = new IPEndPoint(
            family == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any,
            0
        );

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await socket
                    .ReceiveMessageFromAsync(buffer, SocketFlags.None, anyEndPoint, ct)
                    .ConfigureAwait(false);

                if (result.ReceivedBytes > 0)
                {
                    this.Dispatch(
                        buffer.AsMemory(0, result.ReceivedBytes),
                        (IPEndPoint)result.RemoteEndPoint,
                        result.PacketInformation.Interface
                    );
                }
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
                logger.LogDebug(ex, "{Protocol} receive error on {Family}", endpoint.Name, family);
            }
        }
    }


    void Dispatch(ReadOnlyMemory<byte> payload, IPEndPoint source, int interfaceIndex)
    {
        try
        {
            this.DatagramReceived?.Invoke(new MulticastDatagram(
                payload,
                source,
                interfaceIndex,
                this.FindLocalAddress(interfaceIndex, source.AddressFamily)
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A {Protocol} datagram handler threw", endpoint.Name);
        }
    }


    IPAddress? FindLocalAddress(int interfaceIndex, AddressFamily family)
    {
        if (interfaceIndex == 0)
            return null;

        lock (this.joinLock)
        {
            return this.interfaceAddresses.TryGetValue(interfaceIndex, out var addresses)
                ? addresses.FirstOrDefault(x => x.AddressFamily == family)
                : null;
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


    IEnumerable<IPAddress> GetLocalV6Addresses()
        => GetUsableInterfaces()
            .SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .Select(x => x.Address)
            .Where(x => x.AddressFamily == AddressFamily.InterNetworkV6)
            // mDNS advertises routable addresses only; SSDP and WS-Discovery live on the FF02
            // link-local scope and need them
            .Where(x => endpoint.IncludeIPv6LinkLocal || !x.IsIPv6LinkLocal);


    public void Dispose()
    {
        lock (this.stateLock)
        {
            this.refCount = 0;
            this.Stop();
        }
        this.sendLock.Dispose();
    }


    readonly record struct V4Membership(IPAddress LocalAddress, int InterfaceIndex);


    sealed class Lease(MulticastSocketSet client) : IDisposable
    {
        int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 0)
                client.Release();
        }
    }
}
