using System.Net;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed.WsDiscovery;


/// <summary>
/// A live WS-Discovery target service: Hello on start, answers to matching Probe and Resolve
/// messages, and Bye on dispose.
/// </summary>
sealed class WsdPublication : IWsDiscoveryPublication
{
    const int ResponseBurst = 10;
    const int ResponsesPerSecond = 5;
    const int MaxTrackedSources = 256;

    readonly MulticastSocketSet sockets;
    readonly WsDiscoveryRegistration registration;
    readonly ILogger logger;
    readonly IDisposable lease;
    readonly Dictionary<IPAddress, TokenBucket> responseBudget = new();
    readonly Lock sync = new();
    readonly CancellationTokenSource cancelSource = new();
    readonly uint instanceId;

    uint messageNumber;
    uint metadataVersion;
    int disposed;


    WsdPublication(
        MulticastSocketSet sockets,
        WsDiscoveryRegistration registration,
        ILogger logger,
        IDisposable lease
    )
    {
        this.sockets = sockets;
        this.registration = registration;
        this.logger = logger;
        this.lease = lease;
        this.metadataVersion = registration.MetadataVersion;

        // must increase across restarts so clients can tell a reboot from a duplicate
        this.instanceId = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() & 0x7FFFFFFF);
    }


    public string EndpointReference => this.registration.EndpointReference;

    public uint MetadataVersion
    {
        get
        {
            lock (this.sync)
                return this.metadataVersion;
        }
    }


    public static async Task<IWsDiscoveryPublication> Create(
        MulticastSocketSet sockets,
        WsDiscoveryRegistration registration,
        ILogger logger,
        CancellationToken ct
    )
    {
        var epr = WsdEnvelopeReader.NormalizeEndpointReference(registration.EndpointReference ?? String.Empty);
        if (String.IsNullOrWhiteSpace(epr))
            throw new WsDiscoveryException("The registration needs an endpoint reference, normally \"urn:uuid:\" followed by a GUID.");

        if (registration.Profiles == WsDiscoveryProfile.None)
            throw new WsDiscoveryException("At least one WS-Discovery profile must be selected.");

        var normalized = registration with { EndpointReference = epr };
        var lease = sockets.Acquire();
        var publication = new WsdPublication(sockets, normalized, logger, lease);
        try
        {
            sockets.DatagramReceived += publication.OnDatagramReceived;
            sockets.NetworkChanged += publication.OnNetworkChanged;

            await publication.SendAnnouncement(WsdMessageNames.Hello, ct).ConfigureAwait(false);
            return publication;
        }
        catch
        {
            await publication.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }


    async Task SendAnnouncement(string messageName, CancellationToken ct)
    {
        var match = this.BuildMatch();

        foreach (var profile in WsdVersionProfile.Selected(this.registration.Profiles))
        {
            var messageId = $"urn:uuid:{Guid.NewGuid():D}";
            var sequence = this.NextSequence();

            var payload = messageName.Equals(WsdMessageNames.Hello, StringComparison.Ordinal)
                ? WsdEnvelopeWriter.Hello(profile, messageId, sequence, match)
                : WsdEnvelopeWriter.Bye(profile, messageId, sequence, match);

            // an unsolicited announcement is jittered so a rack of devices coming up together
            // does not produce a synchronised burst
            await Task
                .Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(0, (int)WsdTransport.AppMaxDelay.TotalMilliseconds)), ct)
                .ConfigureAwait(false);

            await this.sockets.SendMulticast(payload, ct).ConfigureAwait(false);
        }
    }


    void OnNetworkChanged()
    {
        lock (this.sync)
            this.metadataVersion++;

        _ = Task.Run(async () =>
        {
            try
            {
                await this.SendAnnouncement(WsdMessageNames.Hello, this.cancelSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogDebug(ex, "Could not re-announce {Epr} after a network change", this.EndpointReference);
            }
        });
    }


    void OnDatagramReceived(MulticastDatagram datagram)
    {
        var message = WsdEnvelopeReader.TryParse(datagram.Payload.Span);
        if (message == null)
            return;

        // answering off-link sources is what turns a responder into a reflector
        if (!LocalNetworkGuard.IsOnLocalNetwork(datagram.Source.Address))
            return;

        var responseName = message.MessageName switch
        {
            WsdMessageNames.Probe when this.MatchesProbe(message) => WsdMessageNames.ProbeMatches,
            WsdMessageNames.Resolve when this.MatchesResolve(message) => WsdMessageNames.ResolveMatches,
            _ => null
        };

        if (responseName == null || message.MessageId == null)
            return;

        if (!this.TryTakeBudget(datagram.Source.Address))
        {
            this.logger.LogDebug("Rate limited a WS-Discovery request from {Source}", datagram.Source);
            return;
        }

        _ = Task.Run(() => this.RespondAsync(message, responseName, datagram.Source));
    }


    bool MatchesProbe(WsdMessage message)
    {
        var criteria = message.Probe;
        if (criteria == null)
            return false;

        return WsdScopeMatcher.MatchesTypes(criteria.Types, this.registration.Types) &&
            WsdScopeMatcher.MatchesScopes(criteria.Scopes, this.registration.Scopes, criteria.MatchBy, message.Profile);
    }


    bool MatchesResolve(WsdMessage message)
        => message.ResolveTarget != null &&
            message.ResolveTarget.Equals(this.EndpointReference, StringComparison.OrdinalIgnoreCase);


    async Task RespondAsync(WsdMessage request, string responseName, IPEndPoint destination)
    {
        try
        {
            // SOAP-over-UDP APP_MAX_DELAY, so a network full of targets does not answer in unison
            await Task
                .Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(0, (int)WsdTransport.AppMaxDelay.TotalMilliseconds)), this.cancelSource.Token)
                .ConfigureAwait(false);

            var payload = WsdEnvelopeWriter.Matches(
                // always answer in the version the request arrived in
                request.Profile,
                responseName,
                $"urn:uuid:{Guid.NewGuid():D}",
                request.MessageId!,
                this.NextSequence(),
                new[] { this.BuildMatch() }
            );

            await this.sockets.SendTo(payload, destination, this.cancelSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // publication disposed mid-response
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Failed to answer a WS-Discovery request from {Destination}", destination);
        }
    }


    WsdMatch BuildMatch() => new(
        this.EndpointReference,
        this.registration.Types,
        this.registration.Scopes,
        this.registration.Addresses.Select(x => x.AbsoluteUri).ToList(),
        this.MetadataVersion
    );


    WsdAppSequence NextSequence()
    {
        lock (this.sync)
            return new WsdAppSequence(this.instanceId, null, ++this.messageNumber);
    }


    bool TryTakeBudget(IPAddress source)
    {
        lock (this.sync)
        {
            if (!this.responseBudget.TryGetValue(source, out var bucket))
            {
                if (this.responseBudget.Count >= MaxTrackedSources)
                    this.responseBudget.Clear();

                bucket = new TokenBucket();
                this.responseBudget[source] = bucket;
            }
            return bucket.TryTake();
        }
    }


    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) != 0)
            return;

        this.sockets.DatagramReceived -= this.OnDatagramReceived;
        this.sockets.NetworkChanged -= this.OnNetworkChanged;

        try
        {
            await this.SendAnnouncement(WsdMessageNames.Bye, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Could not send a WS-Discovery Bye for {Epr}", this.EndpointReference);
        }

        await this.cancelSource.CancelAsync().ConfigureAwait(false);
        this.cancelSource.Dispose();
        this.lease.Dispose();
    }


    sealed class TokenBucket
    {
        double tokens = ResponseBurst;
        long lastRefill = Environment.TickCount64;

        public bool TryTake()
        {
            var now = Environment.TickCount64;
            var elapsed = (now - this.lastRefill) / 1000d;
            this.lastRefill = now;

            this.tokens = Math.Min(ResponseBurst, this.tokens + (elapsed * ResponsesPerSecond));

            if (this.tokens < 1)
                return false;

            this.tokens -= 1;
            return true;
        }
    }
}
