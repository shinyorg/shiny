using System.Net;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed.Ssdp;


/// <summary>
/// A live SSDP advertisement: the initial announcement burst, periodic re-announcements, replies
/// to matching searches, and goodbyes on dispose.
/// </summary>
sealed class SsdpPublication : ISsdpPublication
{
    /// <summary>UDA requires each announcement to be sent more than once, since UDP loses datagrams.</summary>
    const int AnnounceTransmissions = 3;
    static readonly TimeSpan announceGap = TimeSpan.FromMilliseconds(150);

    /// <summary>Per-source response allowance. SSDP reflection is a real amplification vector.</summary>
    const int ResponseBurst = 10;
    const int ResponsesPerSecond = 5;
    const int MaxTrackedSources = 256;

    readonly MulticastSocketSet sockets;
    readonly SsdpDeviceRegistration registration;
    readonly ILogger logger;
    readonly IDisposable lease;
    readonly List<string> notificationTypes;
    readonly Dictionary<IPAddress, TokenBucket> responseBudget = new();
    readonly Lock sync = new();
    readonly CancellationTokenSource cancelSource = new();

    uint bootId;
    int disposed;


    SsdpPublication(
        MulticastSocketSet sockets,
        SsdpDeviceRegistration registration,
        List<string> notificationTypes,
        ILogger logger,
        IDisposable lease
    )
    {
        this.sockets = sockets;
        this.registration = registration;
        this.notificationTypes = notificationTypes;
        this.logger = logger;
        this.lease = lease;

        // must increase across restarts, so it is seeded from the clock rather than randomly
        this.bootId = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() & 0x7FFFFFFF);
    }


    public string Udn => this.registration.Udn;

    public Uri Location => this.registration.Location;

    public IReadOnlyList<string> NotificationTypes => this.notificationTypes;

    public uint BootId
    {
        get
        {
            lock (this.sync)
                return this.bootId;
        }
    }


    public static async Task<ISsdpPublication> Create(
        MulticastSocketSet sockets,
        SsdpDeviceRegistration registration,
        ILogger logger,
        CancellationToken ct
    )
    {
        var udn = UsnParser.NormalizeUdn(registration.Udn);
        if (udn.Length == 0)
            throw new SsdpException("The registration needs a UDN, normally \"uuid:\" followed by a GUID.");

        if (!registration.Location.IsAbsoluteUri)
            throw new SsdpException($"The location '{registration.Location}' must be an absolute URL to the device description document.");

        var normalized = registration with { Udn = udn };
        var types = BuildNotificationTypes(normalized);
        if (types.Count == 0)
            throw new SsdpException("Nothing to advertise - set IsRootDevice, a DeviceType, or at least one ServiceType.");

        var lease = sockets.Acquire();
        var publication = new SsdpPublication(sockets, normalized, types, logger, lease);
        try
        {
            sockets.DatagramReceived += publication.OnDatagramReceived;
            sockets.NetworkChanged += publication.OnNetworkChanged;

            await publication.Announce(ct).ConfigureAwait(false);
            _ = Task.Run(() => publication.RefreshLoop(publication.cancelSource.Token), CancellationToken.None);

            return publication;
        }
        catch
        {
            await publication.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }


    /// <summary>
    /// Builds the advertisement set a UPnP device is required to send: the root marker, the UDN
    /// itself, its device type, and each of its service types.
    /// </summary>
    static List<string> BuildNotificationTypes(SsdpDeviceRegistration registration)
    {
        var types = new List<string>();

        if (registration.IsRootDevice)
            types.Add(SsdpConstants.RootDevice);

        types.Add(registration.Udn);

        if (!String.IsNullOrWhiteSpace(registration.DeviceType))
            types.Add(registration.DeviceType.Trim());

        foreach (var service in registration.ServiceTypes)
        {
            if (!String.IsNullOrWhiteSpace(service))
                types.Add(service.Trim());
        }
        return types.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }


    async Task Announce(CancellationToken ct)
    {
        var boot = this.BootId;

        for (var i = 0; i < AnnounceTransmissions && !ct.IsCancellationRequested; i++)
        {
            foreach (var type in this.notificationTypes)
            {
                await this.sockets
                    .SendMulticast(
                        family => SsdpMessageBuilder.Alive(
                            SsdpTransport.HostHeader(family),
                            type,
                            SsdpMatcher.BuildUsn(this.registration.Udn, type),
                            this.registration.Location,
                            this.registration.MaxAge,
                            boot,
                            this.registration.ConfigId,
                            this.registration.Server
                        ),
                        ct
                    )
                    .ConfigureAwait(false);
            }

            if (i < AnnounceTransmissions - 1)
                await Task.Delay(announceGap, ct).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// Re-announces before the advertisement expires. UDA wants this at a random point under half
    /// of max-age so a network full of devices does not synchronise into bursts.
    /// </summary>
    async Task RefreshLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var half = this.registration.MaxAge.TotalMilliseconds / 2;
                var delay = TimeSpan.FromMilliseconds(half * (0.5 + (Random.Shared.NextDouble() * 0.5)));

                await Task.Delay(delay, ct).ConfigureAwait(false);
                await this.Announce(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // publication disposed
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "The SSDP re-announcement loop for {Udn} failed", this.registration.Udn);
        }
    }


    /// <summary>
    /// A changed address set means a new boot session as far as UPnP is concerned, so the boot id
    /// goes up and everything is announced again.
    /// </summary>
    void OnNetworkChanged()
    {
        lock (this.sync)
            this.bootId = this.bootId >= Int32.MaxValue ? 1 : this.bootId + 1;

        _ = Task.Run(async () =>
        {
            try
            {
                await this.Announce(this.cancelSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogDebug(ex, "Could not re-announce {Udn} after a network change", this.registration.Udn);
            }
        });
    }


    void OnDatagramReceived(MulticastDatagram datagram)
    {
        if (!SsdpMessage.TryParse(datagram.Payload.Span, out var message) || message == null)
            return;

        if (message.Kind != SsdpMessageKind.Search)
            return;

        // MAN is required; its absence marks a malformed or probing sender
        var man = message["MAN"]?.Trim().Trim('"');
        if (!String.Equals(man, "ssdp:discover", StringComparison.OrdinalIgnoreCase))
            return;

        // never answer off-link sources - that is what turns an SSDP responder into a reflector
        if (!LocalNetworkGuard.IsOnLocalNetwork(datagram.Source.Address))
        {
            this.logger.LogDebug("Ignoring an SSDP search from {Source}, which is not on a local network", datagram.Source);
            return;
        }

        var searchTarget = message["ST"]?.Trim();
        var matches = this.notificationTypes
            .Where(x => SsdpMatcher.Matches(searchTarget, x))
            .ToList();

        if (matches.Count == 0)
            return;

        if (!this.TryTakeBudget(datagram.Source.Address, matches.Count))
        {
            this.logger.LogDebug("Rate limited an SSDP search from {Source}", datagram.Source);
            return;
        }

        var mx = Math.Clamp((int)(message.GetUInt("MX") ?? 3), 1, 5);
        _ = Task.Run(() => this.RespondAsync(datagram.Source, searchTarget!, matches, mx));
    }


    async Task RespondAsync(IPEndPoint destination, string searchTarget, List<string> matches, int maxWaitSeconds)
    {
        try
        {
            var isSearchAll = searchTarget.Equals(SsdpConstants.SearchAll, StringComparison.OrdinalIgnoreCase);
            var boot = this.BootId;

            foreach (var type in matches)
            {
                // spread the burst over the window the searcher allowed rather than replying
                // to everything at once
                await Task
                    .Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(0, maxWaitSeconds * 1000)), this.cancelSource.Token)
                    .ConfigureAwait(false);

                // ssdp:all is answered per advertisement; a targeted search echoes what was asked
                // for, which is what makes the version downgrade rule work
                var responseTarget = isSearchAll ? type : searchTarget;

                var payload = SsdpMessageBuilder.SearchResponse(
                    responseTarget,
                    SsdpMatcher.BuildUsn(this.registration.Udn, responseTarget),
                    this.registration.Location,
                    this.registration.MaxAge,
                    boot,
                    this.registration.ConfigId,
                    this.registration.Server
                );

                await this.sockets.SendTo(payload, destination, this.cancelSource.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // publication disposed mid-response
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Failed to answer an SSDP search from {Destination}", destination);
        }
    }


    bool TryTakeBudget(IPAddress source, int count)
    {
        lock (this.sync)
        {
            if (!this.responseBudget.TryGetValue(source, out var bucket))
            {
                // a spoofing sender could otherwise make us allocate a bucket per fake address
                if (this.responseBudget.Count >= MaxTrackedSources)
                    this.responseBudget.Clear();

                bucket = new TokenBucket();
                this.responseBudget[source] = bucket;
            }
            return bucket.TryTake(count);
        }
    }


    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) != 0)
            return;

        this.sockets.DatagramReceived -= this.OnDatagramReceived;
        this.sockets.NetworkChanged -= this.OnNetworkChanged;
        await this.cancelSource.CancelAsync().ConfigureAwait(false);

        try
        {
            var boot = this.BootId;
            foreach (var type in this.notificationTypes)
            {
                await this.sockets
                    .SendMulticast(
                        family => SsdpMessageBuilder.ByeBye(
                            SsdpTransport.HostHeader(family),
                            type,
                            SsdpMatcher.BuildUsn(this.registration.Udn, type),
                            boot,
                            this.registration.ConfigId
                        ),
                        CancellationToken.None
                    )
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Could not send SSDP goodbyes for {Udn}", this.registration.Udn);
        }

        this.cancelSource.Dispose();
        this.lease.Dispose();
    }


    /// <summary>
    /// A simple refilling allowance so one sender cannot make us flood a third party.
    /// </summary>
    sealed class TokenBucket
    {
        double tokens = ResponseBurst;
        long lastRefill = Environment.TickCount64;

        public bool TryTake(int count)
        {
            var now = Environment.TickCount64;
            var elapsed = (now - this.lastRefill) / 1000d;
            this.lastRefill = now;

            this.tokens = Math.Min(ResponseBurst, this.tokens + (elapsed * ResponsesPerSecond));

            if (this.tokens < count)
                return false;

            this.tokens -= count;
            return true;
        }
    }
}
