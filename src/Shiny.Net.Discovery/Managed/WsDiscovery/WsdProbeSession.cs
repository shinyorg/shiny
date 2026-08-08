using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Xml;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed.WsDiscovery;


/// <summary>
/// One live WS-Discovery browse. Probes on an interval in every selected version, folds Hello,
/// ProbeMatches and ResolveMatches into a target cache, and pushes changes onto a channel.
/// </summary>
sealed class WsdProbeSession(
    MulticastSocketSet sockets,
    WsdProbeConfig config,
    ILogger logger
)
{
    static readonly TimeSpan tickInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>A spoofing sender can invent unlimited endpoint references, so the cache is bounded.</summary>
    const int MaxTargets = 512;

    /// <summary>Retransmissions and a device's own repeats both arrive; both dedupe on MessageID.</summary>
    const int MaxSeenMessageIds = 512;

    readonly Lock sync = new();
    readonly Dictionary<string, TargetState> targets = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> ownMessageIds = new(StringComparer.OrdinalIgnoreCase);
    readonly LinkedList<string> seenMessageIds = new();
    readonly HashSet<string> seenMessageIdSet = new(StringComparer.OrdinalIgnoreCase);
    readonly Channel<WsDiscoveryResult> channel = Channel.CreateUnbounded<WsDiscoveryResult>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );


    public async IAsyncEnumerable<WsDiscoveryResult> Run([EnumeratorCancellation] CancellationToken ct)
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
        var nextProbe = 0L;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (Environment.TickCount64 >= nextProbe)
                {
                    await this.SendProbe(ct).ConfigureAwait(false);
                    nextProbe = Environment.TickCount64 + (long)config.ProbeInterval.TotalMilliseconds;
                }

                await this.ResolvePending(ct).ConfigureAwait(false);
                await Task.Delay(tickInterval, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // browse stopped
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The WS-Discovery browse loop failed");
            this.channel.Writer.TryComplete(ex);
        }
    }


    async Task SendProbe(CancellationToken ct)
    {
        foreach (var profile in WsdVersionProfile.Selected(config.Profiles))
        {
            // one message id for all retransmissions of the same logical probe - that is what
            // lets a receiver tell a repeat from a new request
            var messageId = this.NewMessageId();
            var payload = WsdEnvelopeWriter.Probe(
                profile,
                messageId,
                config.Types,
                config.Scopes,
                config.ScopeMatchBy
            );

            await this.Transmit(payload, ct).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// SOAP-over-UDP retransmission: send, wait a random delay, double the window, repeat.
    /// </summary>
    async Task Transmit(byte[] payload, CancellationToken ct)
    {
        var min = WsdTransport.UdpMinDelay;
        var max = WsdTransport.UdpMaxDelay;

        for (var i = 0; i < WsdTransport.MulticastTransmissions && !ct.IsCancellationRequested; i++)
        {
            await sockets.SendQuery(payload, ct).ConfigureAwait(false);

            if (i < WsdTransport.MulticastTransmissions - 1)
            {
                var delay = Random.Shared.Next((int)min.TotalMilliseconds, (int)max.TotalMilliseconds + 1);
                await Task.Delay(TimeSpan.FromMilliseconds(delay), ct).ConfigureAwait(false);

                min = TimeSpan.FromMilliseconds(Math.Min(min.TotalMilliseconds * 2, WsdTransport.UdpUpperDelay.TotalMilliseconds));
                max = TimeSpan.FromMilliseconds(Math.Min(max.TotalMilliseconds * 2, WsdTransport.UdpUpperDelay.TotalMilliseconds));
            }
        }
    }


    /// <summary>
    /// Asks targets that answered without a transport address to supply one.
    /// </summary>
    async Task ResolvePending(CancellationToken ct)
    {
        if (!config.ResolveMissingAddresses)
            return;

        List<TargetState> pending;
        lock (this.sync)
        {
            pending = this.targets.Values
                .Where(x => x.Addresses.Count == 0 && !x.ResolveSent)
                .ToList();

            foreach (var state in pending)
                state.ResolveSent = true;
        }

        foreach (var state in pending)
        {
            var profile = WsdVersionProfile.All.FirstOrDefault(x => x.Profile == state.Profile) ?? WsdVersionProfile.Ws2005;
            var payload = WsdEnvelopeWriter.Resolve(profile, this.NewMessageId(), state.EndpointReference);

            await sockets.SendQuery(payload, ct).ConfigureAwait(false);
        }
    }


    void OnDatagramReceived(MulticastDatagram datagram)
    {
        var message = WsdEnvelopeReader.TryParse(datagram.Payload.Span);
        if (message == null)
            return;

        // our own probes come straight back through multicast loopback
        if (message.MessageId != null && this.IsOwnMessage(message.MessageId))
            return;

        if (message.MessageId != null && !this.TryMarkSeen(message.MessageId))
            return;

        switch (message.MessageName)
        {
            case WsdMessageNames.Bye:
                this.ApplyBye(message);
                break;

            case WsdMessageNames.Hello when config.ListenForAnnouncements:
            case WsdMessageNames.ProbeMatches:
            case WsdMessageNames.ResolveMatches:
                this.ApplyMatches(message, datagram);
                break;
        }
    }


    void ApplyMatches(WsdMessage message, MulticastDatagram datagram)
    {
        foreach (var match in message.Matches)
        {
            // devices commonly answer every probe regardless of what was asked, so the filter is
            // applied here as well as by the responder
            var matchesTypes = WsdScopeMatcher.MatchesTypes(config.Types, match.Types);
            var matchesScopes = WsdScopeMatcher.MatchesScopes(config.Scopes, match.Scopes, config.ScopeMatchBy, message.Profile);

            if (matchesTypes && matchesScopes)
                this.Upsert(message, match, datagram);
        }
    }


    void Upsert(WsdMessage message, WsdMatch match, MulticastDatagram datagram)
    {
        var addresses = WsdTransport.ParseAddresses(match.XAddrs);

        lock (this.sync)
        {
            if (!this.targets.TryGetValue(match.EndpointReference, out var state))
            {
                if (this.targets.Count >= MaxTargets)
                {
                    logger.LogWarning("Ignoring WS-Discovery target {Epr} - the browse cache is full at {Max}", match.EndpointReference, MaxTargets);
                    return;
                }

                state = new TargetState(match.EndpointReference);
                this.targets[match.EndpointReference] = state;
            }

            if (!state.AcceptSequence(message.AppSequence))
            {
                logger.LogTrace("Dropped an out of order WS-Discovery message for {Epr}", match.EndpointReference);
                return;
            }

            state.Types = match.Types;
            state.Scopes = match.Scopes;
            state.MetadataVersion = match.MetadataVersion;
            state.Profile = message.Profile.Profile;
            state.SourceAddress = datagram.Source.Address;
            state.InterfaceIndex = datagram.InterfaceIndex;

            if (addresses.Count > 0)
            {
                state.Addresses = addresses;
                state.ResolveSent = false;
            }

            this.EmitIfChanged(state);
        }
    }


    void ApplyBye(WsdMessage message)
    {
        foreach (var match in message.Matches)
        {
            lock (this.sync)
            {
                if (!this.targets.TryGetValue(match.EndpointReference, out var state))
                    return;

                // a Bye from a previous instance must not kill a device that already came back
                if (!state.AcceptSequence(message.AppSequence))
                {
                    logger.LogTrace("Ignoring a stale WS-Discovery Bye for {Epr}", match.EndpointReference);
                    return;
                }

                this.targets.Remove(match.EndpointReference);

                if (state.Emitted)
                {
                    this.Enqueue(new WsDiscoveryResult(
                        WsDiscoveryStatus.Lost,
                        new WsdTarget { EndpointReference = state.EndpointReference }
                    ));
                }
            }
        }
    }


    /// <summary>Callers must hold <see cref="sync"/>.</summary>
    void EmitIfChanged(TargetState state)
    {
        var target = state.Build();
        var signature = Signature(target);

        if (!state.Emitted || state.Signature != signature)
        {
            state.Emitted = true;
            state.Signature = signature;
            this.Enqueue(new WsDiscoveryResult(WsDiscoveryStatus.Found, target));
        }
    }


    static string Signature(WsdTarget target)
        => String.Join(
            '|',
            target.MetadataVersion,
            target.PreferredAddress?.AbsoluteUri,
            String.Join(',', target.Addresses.Select(x => x.AbsoluteUri).Order(StringComparer.Ordinal)),
            String.Join(',', target.Types.Select(x => x.ToString()).Order(StringComparer.Ordinal)),
            String.Join(',', target.Scopes.Order(StringComparer.Ordinal))
        );


    string NewMessageId()
    {
        var id = $"urn:uuid:{Guid.NewGuid():D}";
        lock (this.sync)
        {
            // bounded so a long lived browse does not grow without limit
            if (this.ownMessageIds.Count > MaxSeenMessageIds)
                this.ownMessageIds.Clear();

            this.ownMessageIds.Add(id);
        }
        return id;
    }


    bool IsOwnMessage(string messageId)
    {
        lock (this.sync)
            return this.ownMessageIds.Contains(messageId);
    }


    /// <summary>
    /// Records a message id, returning false when it has already been handled. Both our own
    /// retransmissions and the device's repeats bring the same id round more than once.
    /// </summary>
    bool TryMarkSeen(string messageId)
    {
        lock (this.sync)
        {
            if (!this.seenMessageIdSet.Add(messageId))
                return false;

            this.seenMessageIds.AddLast(messageId);
            if (this.seenMessageIds.Count > MaxSeenMessageIds)
            {
                var oldest = this.seenMessageIds.First!.Value;
                this.seenMessageIds.RemoveFirst();
                this.seenMessageIdSet.Remove(oldest);
            }
            return true;
        }
    }


    void Enqueue(WsDiscoveryResult result)
    {
        if (!this.channel.Writer.TryWrite(result))
            logger.LogWarning("Dropped a WS-Discovery result for {Epr} - the consumer is not reading", result.Target.EndpointReference);
    }


    sealed class TargetState(string endpointReference)
    {
        public string EndpointReference { get; } = endpointReference;

        public IReadOnlyList<XmlQualifiedName> Types { get; set; } = Array.Empty<XmlQualifiedName>();

        public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();

        public IReadOnlyList<Uri> Addresses { get; set; } = Array.Empty<Uri>();

        public uint MetadataVersion { get; set; }

        public IPAddress? SourceAddress { get; set; }

        public int InterfaceIndex { get; set; }

        public WsDiscoveryProfile Profile { get; set; }

        public bool ResolveSent { get; set; }

        public bool Emitted { get; set; }

        public string? Signature { get; set; }

        uint? instanceId;
        uint messageNumber;


        /// <summary>
        /// Applies AppSequence ordering. A message from an older instance, or an older message
        /// within the current one, is a straggler and must not overwrite newer state.
        /// </summary>
        public bool AcceptSequence(WsdAppSequence? sequence)
        {
            // cheap hardware omits AppSequence entirely, so its absence cannot be a rejection
            if (sequence == null)
                return true;

            var incoming = sequence.Value;

            if (this.instanceId != null)
            {
                if (incoming.InstanceId < this.instanceId)
                    return false;

                if (incoming.InstanceId == this.instanceId && incoming.MessageNumber <= this.messageNumber)
                    return false;
            }

            this.instanceId = incoming.InstanceId;
            this.messageNumber = incoming.MessageNumber;
            return true;
        }


        public WsdTarget Build()
        {
            var source = this.SourceAddress ?? IPAddress.None;

            return new WsdTarget
            {
                EndpointReference = this.EndpointReference,
                Types = this.Types,
                Scopes = this.Scopes,
                Addresses = this.Addresses,
                PreferredAddress = WsdTransport.ChoosePreferred(this.Addresses, source, this.InterfaceIndex),
                MetadataVersion = this.MetadataVersion,
                SourceAddress = this.SourceAddress,
                InterfaceIndex = this.InterfaceIndex,
                Profile = this.Profile
            };
        }
    }
}
