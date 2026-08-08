using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed.Ssdp;


/// <summary>
/// One live SSDP browse. Sends M-SEARCH on an interval, folds every advertisement into a
/// per-device cache, and pushes appear/disappear changes onto a channel.
/// </summary>
sealed class SsdpBrowseSession(
    MulticastSocketSet sockets,
    SsdpBrowseConfig config,
    ILogger logger
)
{
    /// <summary>How often expiries are checked.</summary>
    static readonly TimeSpan tickInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>UDP loses datagrams, so each search goes out more than once.</summary>
    const int SearchTransmissions = 2;
    static readonly TimeSpan retransmitDelay = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Slack added to a device's max-age before we call it lost, so one dropped re-announcement
    /// does not evict a device that is still there.
    /// </summary>
    static readonly TimeSpan expiryGrace = TimeSpan.FromSeconds(10);

    /// <summary>A spoofing sender can invent unlimited UDNs, so the cache is bounded.</summary>
    const int MaxDevices = 512;

    readonly Lock sync = new();
    readonly Dictionary<string, DeviceState> devices = new(StringComparer.OrdinalIgnoreCase);
    readonly Channel<SsdpBrowseResult> channel = Channel.CreateUnbounded<SsdpBrowseResult>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );


    public async IAsyncEnumerable<SsdpBrowseResult> Run([EnumeratorCancellation] CancellationToken ct)
    {
        using var lease = sockets.Acquire();
        sockets.DatagramReceived += this.OnDatagramReceived;

        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pump = Task.Run(() => this.PumpAsync(cancel.Token), cancel.Token);
        try
        {
            await foreach (var result in this.channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                yield return result;
        }
        finally
        {
            sockets.DatagramReceived -= this.OnDatagramReceived;
            await cancel.CancelAsync().ConfigureAwait(false);
            await Task.WhenAny(pump, Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None)).ConfigureAwait(false);
        }
    }


    async Task PumpAsync(CancellationToken ct)
    {
        var nextSearch = 0L;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (Environment.TickCount64 >= nextSearch)
                {
                    await this.SendSearch(ct).ConfigureAwait(false);
                    nextSearch = Environment.TickCount64 + (long)config.SearchInterval.TotalMilliseconds;
                }

                this.Sweep();
                await Task.Delay(tickInterval, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // browse stopped
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The SSDP browse loop for {SearchTarget} failed", config.SearchTarget);
            this.channel.Writer.TryComplete(ex);
        }
    }


    async Task SendSearch(CancellationToken ct)
    {
        // UDA caps MX at 5 and requires at least 1
        var mx = Math.Clamp((int)config.MaxWait.TotalSeconds, 1, 5);

        for (var i = 0; i < SearchTransmissions && !ct.IsCancellationRequested; i++)
        {
            await sockets
                .SendQuery(
                    family => SsdpMessageBuilder.Search(
                        config.SearchTarget,
                        mx,
                        SsdpTransport.HostHeader(family),
                        config.FriendlyName
                    ),
                    ct
                )
                .ConfigureAwait(false);

            if (i < SearchTransmissions - 1)
                await Task.Delay(retransmitDelay, ct).ConfigureAwait(false);
        }
    }


    void OnDatagramReceived(MulticastDatagram datagram)
    {
        if (!SsdpMessage.TryParse(datagram.Payload.Span, out var message) || message == null)
            return;

        switch (message.Kind)
        {
            case SsdpMessageKind.Response:
                this.ApplyAdvertisement(message, datagram, message["ST"]);
                break;

            case SsdpMessageKind.Notify:
                this.ApplyNotify(message, datagram);
                break;

            // an M-SEARCH from someone else on the network is not our business while browsing
            case SsdpMessageKind.Search:
                break;
        }
    }


    void ApplyNotify(SsdpMessage message, MulticastDatagram datagram)
    {
        var nts = message["NTS"]?.Trim();

        if (String.Equals(nts, SsdpConstants.ByeByeNts, StringComparison.OrdinalIgnoreCase))
            this.ApplyByeBye(message);
        else
            this.ApplyAdvertisement(message, datagram, message["NT"]);
    }


    /// <summary>
    /// Folds an alive, update or search response into the device cache.
    /// </summary>
    void ApplyAdvertisement(SsdpMessage message, MulticastDatagram datagram, string? declaredType)
    {
        if (!UsnParser.TryParse(message["USN"], out var usn))
            return;

        // the USN's own suffix is authoritative; NT/ST is the fallback for devices that omit it
        var notificationType = usn.NotificationType ?? declaredType?.Trim();

        // devices that answer every search regardless of ST get filtered here
        if (!SsdpMatcher.Matches(config.SearchTarget, notificationType ?? usn.Udn))
            return;

        var bootId = message.GetUInt("BOOTID.UPNP.ORG");
        var nextBootId = message.GetUInt("NEXTBOOTID.UPNP.ORG");
        var maxAge = message.GetMaxAge(SsdpConstants.DefaultMaxAge);
        var location = ResolveLocation(message["LOCATION"], datagram);

        lock (this.sync)
        {
            if (!this.devices.TryGetValue(usn.Udn, out var state))
            {
                if (this.devices.Count >= MaxDevices)
                {
                    logger.LogWarning("Ignoring SSDP device {Udn} - the browse cache is full at {Max}", usn.Udn, MaxDevices);
                    return;
                }

                state = new DeviceState(usn.Udn);
                this.devices[usn.Udn] = state;
            }

            // a datagram from a boot session older than the one we track is stale by definition
            if (bootId != null && state.BootId != null && bootId < state.BootId)
                return;

            state.BootId = nextBootId ?? bootId ?? state.BootId;
            state.ConfigId = message.GetUInt("CONFIGID.UPNP.ORG") ?? state.ConfigId;
            state.SearchPort = (int?)message.GetUInt("SEARCHPORT.UPNP.ORG") ?? state.SearchPort;
            state.Server = message["SERVER"] ?? state.Server;
            state.Location = location ?? state.Location;
            state.SourceAddress = datagram.Source.Address;
            state.InterfaceIndex = datagram.InterfaceIndex;
            state.Headers = message.Headers;

            var type = notificationType ?? usn.Udn;
            state.Advertisements[type] = Environment.TickCount64 + (long)(maxAge + expiryGrace).TotalMilliseconds;

            this.EmitIfChanged(state);
        }
    }


    void ApplyByeBye(SsdpMessage message)
    {
        if (!UsnParser.TryParse(message["USN"], out var usn))
            return;

        var bootId = message.GetUInt("BOOTID.UPNP.ORG");

        lock (this.sync)
        {
            if (!this.devices.TryGetValue(usn.Udn, out var state))
                return;

            // a goodbye from the previous boot session must not kill a device that already came
            // back - this race is common when a device reboots quickly
            if (bootId != null && state.BootId != null && bootId != state.BootId)
            {
                logger.LogTrace("Ignoring a stale SSDP goodbye for {Udn} from boot {BootId}", usn.Udn, bootId);
                return;
            }

            var type = usn.NotificationType ?? message["NT"]?.Trim() ?? usn.Udn;
            var isWholeDevice = type.Equals(SsdpConstants.RootDevice, StringComparison.OrdinalIgnoreCase) ||
                type.Equals(usn.Udn, StringComparison.OrdinalIgnoreCase);

            if (isWholeDevice)
                state.Advertisements.Clear();
            else
                state.Advertisements.Remove(type);

            if (state.Advertisements.Count == 0)
                this.Remove(state);
            else
                this.EmitIfChanged(state);
        }
    }


    /// <summary>
    /// Turns a raw LOCATION header into a usable URL. Whether it may actually be fetched is
    /// decided later, by <see cref="UpnpDescriptionFetcher"/> - the address is still worth
    /// surfacing on the device either way.
    /// </summary>
    static Uri? ResolveLocation(string? raw, MulticastDatagram datagram)
        => String.IsNullOrWhiteSpace(raw) || !Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var location)
            ? null
            // a link-local IPv6 host without a zone id cannot be connected to
            : LocalNetworkGuard.ApplyZoneId(location, datagram.InterfaceIndex);


    /// <summary>
    /// Drops advertisements that aged out and emits Lost for devices left with none.
    /// </summary>
    void Sweep()
    {
        lock (this.sync)
        {
            var now = Environment.TickCount64;

            foreach (var state in this.devices.Values.ToList())
            {
                var expired = state.Advertisements
                    .Where(x => x.Value <= now)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var type in expired)
                    state.Advertisements.Remove(type);

                if (state.Advertisements.Count == 0)
                    this.Remove(state);
            }
        }
    }


    /// <summary>Callers must hold <see cref="sync"/>.</summary>
    void Remove(DeviceState state)
    {
        this.devices.Remove(state.Udn);

        if (state.Emitted)
            this.Enqueue(new SsdpBrowseResult(SsdpBrowseStatus.Lost, new SsdpDevice { Udn = state.Udn }));
    }


    /// <summary>Callers must hold <see cref="sync"/>.</summary>
    void EmitIfChanged(DeviceState state)
    {
        var device = state.Build();
        var signature = Signature(device);

        if (!state.Emitted || state.Signature != signature)
        {
            state.Emitted = true;
            state.Signature = signature;
            this.Enqueue(new SsdpBrowseResult(SsdpBrowseStatus.Found, device));
        }
    }


    static string Signature(SsdpDevice device)
        => String.Join(
            '|',
            device.Location?.AbsoluteUri,
            device.BootId,
            device.ConfigId,
            device.Server,
            String.Join(',', device.NotificationTypes.Order(StringComparer.OrdinalIgnoreCase))
        );


    void Enqueue(SsdpBrowseResult result)
    {
        if (!this.channel.Writer.TryWrite(result))
            logger.LogWarning("Dropped an SSDP browse result for {Udn} - the consumer is not reading", result.Device.Udn);
    }


    sealed class DeviceState(string udn)
    {
        public string Udn { get; } = udn;

        /// <summary>Notification type to its monotonic expiry deadline.</summary>
        public Dictionary<string, long> Advertisements { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Uri? Location { get; set; }

        public string? Server { get; set; }

        public IPAddress? SourceAddress { get; set; }

        public int InterfaceIndex { get; set; }

        public uint? BootId { get; set; }

        public uint? ConfigId { get; set; }

        public int? SearchPort { get; set; }

        public IReadOnlyDictionary<string, string> Headers { get; set; } = SsdpConstants.EmptyHeaders;

        public bool Emitted { get; set; }

        public string? Signature { get; set; }


        public SsdpDevice Build() => new()
        {
            Udn = this.Udn,
            Location = this.Location,
            Server = this.Server,
            NotificationTypes = this.Advertisements.Keys.ToList(),
            SourceAddress = this.SourceAddress,
            InterfaceIndex = this.InterfaceIndex,
            BootId = this.BootId,
            ConfigId = this.ConfigId,
            SearchPort = this.SearchPort,
            Headers = this.Headers
        };
    }
}
