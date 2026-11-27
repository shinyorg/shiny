using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Internals;
using Shiny.Net.Discovery.Managed.Dns;
// .NET 11 added System.Net.PtrRecord/SrvRecord/TxtRecord/AddressRecord - these names mean the mDNS wire records
using AddressRecord = Shiny.Net.Discovery.Managed.Dns.AddressRecord;
using PtrRecord = Shiny.Net.Discovery.Managed.Dns.PtrRecord;
using SrvRecord = Shiny.Net.Discovery.Managed.Dns.SrvRecord;
using TxtRecord = Shiny.Net.Discovery.Managed.Dns.TxtRecord;

namespace Shiny.Net.Discovery.Managed;


/// <summary>
/// One live browse. Sends the PTR query on a backoff, feeds every received record into a small
/// TTL-aware cache, and pushes appear/disappear changes onto a channel.
/// </summary>
/// <remarks>
/// SRV and TXT records are refreshed the way RFC 6762 section 5.2 describes: a question at 80%, 85%,
/// 90% and 95% of the record's TTL (plus up to 2% jitter), dropped when the TTL runs out. An instance
/// whose SRV or TXT ran out while its PTR is still live is asked about once more instead of being
/// reported lost, and keeps its last reported details while that answer is awaited.
/// </remarks>
sealed class MdnsBrowseSession(
    MdnsMulticastClient client,
    ServiceTypeInfo serviceType,
    MdnsBrowseConfig config,
    ILogger logger,
    TimeProvider? timeProvider = null
)
{
    /// <summary>How often the maintenance loop checks expiries and re-issues resolve queries.</summary>
    static readonly TimeSpan tickInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>First PTR re-query delay. RFC 6762 section 5.2 says double it each time.</summary>
    static readonly TimeSpan initialQueryInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// RFC 6762 caps the backoff at 60 minutes. That is far too lazy for a phone walking between
    /// networks, so it is capped at a minute and the network-change rejoin covers the rest.
    /// </summary>
    static readonly TimeSpan maxQueryInterval = TimeSpan.FromSeconds(60);

    readonly Lock sync = new();
    readonly Dictionary<DnsName, InstanceState> instances = new();
    readonly Dictionary<DnsName, CachedRecord<SrvRecord>> srvRecords = new();
    readonly Dictionary<DnsName, CachedRecord<TxtRecord>> txtRecords = new();
    readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    readonly Dictionary<DnsName, Dictionary<IPAddress, DateTimeOffset>> addressRecords = new();

    readonly DnsName typeName = DnsName.ForServiceType(serviceType.Application, serviceType.Protocol, serviceType.Domain);
    readonly Channel<MdnsBrowseResult> channel = Channel.CreateUnbounded<MdnsBrowseResult>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );


    public async IAsyncEnumerable<MdnsBrowseResult> Run([EnumeratorCancellation] CancellationToken ct)
    {
        using var lease = client.Acquire();
        client.MessageReceived += this.OnMessageReceived;

        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pump = Task.Run(() => this.PumpAsync(cancel.Token), cancel.Token);
        try
        {
            await foreach (var result in this.Results.ReadAllAsync(ct).ConfigureAwait(false))
                yield return result;
        }
        finally
        {
            client.MessageReceived -= this.OnMessageReceived;
            await cancel.CancelAsync().ConfigureAwait(false);
            await Task.WhenAny(pump, Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None)).ConfigureAwait(false);
        }
    }


    async Task PumpAsync(CancellationToken ct)
    {
        var nextQuery = this.clock.GetUtcNow();
        var queryInterval = initialQueryInterval;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var now = this.clock.GetUtcNow();
                if (now >= nextQuery)
                {
                    await this.SendTypeQuery(ct).ConfigureAwait(false);
                    nextQuery = now + queryInterval;
                    queryInterval = queryInterval < maxQueryInterval
                        ? TimeSpan.FromTicks(Math.Min(queryInterval.Ticks * 2, maxQueryInterval.Ticks))
                        : maxQueryInterval;
                }

                await this.SendResolveQueries(ct).ConfigureAwait(false);
                this.Sweep();

                await Task.Delay(tickInterval, this.clock, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // browse stopped
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The mDNS browse loop for {ServiceType} failed", serviceType.ServiceType);
            this.channel.Writer.TryComplete(ex);
        }
    }


    async Task SendTypeQuery(CancellationToken ct)
    {
        var query = DnsMessage.CreateQuery();
        query.Questions.Add(new DnsQuestion { Name = this.typeName, Type = DnsRecordType.PTR });

        await client.Send(query, ct).ConfigureAwait(false);
    }


    async Task SendResolveQueries(CancellationToken ct)
    {
        var query = this.BuildResolveQuery();
        if (query != null)
            await client.Send(query, ct).ConfigureAwait(false);
    }


    /// <summary>
    /// The SRV/TXT of every instance we know about but have not resolved (or whose SRV/TXT just
    /// expired), the A/AAAA of every SRV target we have no address for, and the SRV/TXT whose TTL
    /// reached a refresh point. Null when there is nothing to ask.
    /// </summary>
    internal DnsMessage? BuildResolveQuery()
    {
        if (!config.ResolveServices)
            return null;

        var query = DnsMessage.CreateQuery();
        lock (this.sync)
        {
            var now = this.clock.GetUtcNow();
            foreach (var instance in this.instances.Values)
            {
                // once the deadline passes we emit what we have and stop asking - unless a cached
                // record expired under a live PTR, which earns one more question
                var asking = now <= instance.ResolveDeadline || instance.RequeryPending;
                instance.RequeryPending = false;

                this.srvRecords.TryGetValue(instance.FullName, out var srv);
                this.txtRecords.TryGetValue(instance.FullName, out var txt);

                if ((srv == null && asking) || (srv != null && srv.TakeRefresh(now)))
                    query.Questions.Add(new DnsQuestion { Name = instance.FullName, Type = DnsRecordType.SRV });

                if ((txt == null && asking) || (txt != null && txt.TakeRefresh(now)))
                    query.Questions.Add(new DnsQuestion { Name = instance.FullName, Type = DnsRecordType.TXT });

                if (now <= instance.ResolveDeadline && srv != null && !this.HasAddresses(srv.Record.Target))
                {
                    query.Questions.Add(new DnsQuestion { Name = srv.Record.Target, Type = DnsRecordType.A });
                    query.Questions.Add(new DnsQuestion { Name = srv.Record.Target, Type = DnsRecordType.AAAA });
                }
            }
        }
        return query.Questions.Count > 0 ? query : null;
    }


    internal void OnMessageReceived(DnsMessage message, IPEndPoint from)
    {
        lock (this.sync)
        {
            var now = this.clock.GetUtcNow();
            foreach (var record in message.CacheableRecords)
                this.Apply(record, now);

            this.Emit(now);
        }
    }


    /// <summary>What the session has emitted so far - read by <see cref="Run"/>, and by tests directly.</summary>
    internal ChannelReader<MdnsBrowseResult> Results => this.channel.Reader;


    void Apply(DnsResourceRecord record, DateTimeOffset now)
    {
        switch (record)
        {
            case PtrRecord ptr when ptr.Name.Equals(this.typeName):
                this.ApplyPtr(ptr, now);
                break;

            // only instances of the browsed type - every responder on the link answers into the same
            // group, and caching everyone's records grows without bound on a busy network
            case SrvRecord srv when this.IsInstanceName(srv.Name):
                if (srv.IsGoodbye)
                    this.srvRecords.Remove(srv.Name);
                else
                    this.srvRecords[srv.Name] = new CachedRecord<SrvRecord>(srv, now);
                break;

            case TxtRecord txt when this.IsInstanceName(txt.Name):
                if (txt.IsGoodbye)
                    this.txtRecords.Remove(txt.Name);
                else
                    this.txtRecords[txt.Name] = new CachedRecord<TxtRecord>(txt, now);
                break;

            case AddressRecord address:
                this.ApplyAddress(address, now);
                break;
        }
    }


    /// <summary>True for "&lt;instance label&gt;.&lt;the type we browse&gt;".</summary>
    bool IsInstanceName(DnsName name)
        => name.Count == this.typeName.Count + 1 && name.EndsWith(this.typeName);


    void ApplyPtr(PtrRecord ptr, DateTimeOffset now)
    {
        if (ptr.IsGoodbye)
        {
            if (this.instances.Remove(ptr.Target, out var removed) && removed.Emitted)
                this.Enqueue(new MdnsBrowseResult(MdnsBrowseStatus.Lost, this.BuildMinimal(removed)));

            return;
        }

        if (!this.instances.TryGetValue(ptr.Target, out var state))
        {
            // the target must be "<instance label>.<the type we asked about>" - anything else is noise
            if (ptr.Target.Count != this.typeName.Count + 1 || !ptr.Target.EndsWith(this.typeName))
            {
                logger.LogTrace("Ignoring PTR target {Target} that is not an instance of {Type}", ptr.Target, this.typeName);
                return;
            }

            state = new InstanceState
            {
                FullName = ptr.Target,
                InstanceName = ptr.Target[0],
                ResolveDeadline = now + config.ResolveTimeout
            };
            this.instances[ptr.Target] = state;
        }
        state.PtrExpiry = now + TimeSpan.FromSeconds(ptr.Ttl);
    }


    void ApplyAddress(AddressRecord record, DateTimeOffset now)
    {
        if (!this.addressRecords.TryGetValue(record.Name, out var addresses))
        {
            if (record.IsGoodbye)
                return;

            addresses = new Dictionary<IPAddress, DateTimeOffset>();
            this.addressRecords[record.Name] = addresses;
        }

        if (record.IsGoodbye)
            addresses.Remove(record.Address);
        else
            addresses[record.Address] = now + TimeSpan.FromSeconds(record.Ttl);
    }


    bool HasAddresses(DnsName host)
        => this.addressRecords.TryGetValue(host, out var addresses) && addresses.Count > 0;


    /// <summary>
    /// Drops expired records, emits Lost for instances whose PTR aged out, and marks instances
    /// whose SRV/TXT aged out under a live PTR for one more question.
    /// </summary>
    internal void Sweep()
    {
        lock (this.sync)
        {
            var now = this.clock.GetUtcNow();

            foreach (var addresses in this.addressRecords.Values)
            {
                foreach (var expired in addresses.Where(x => x.Value <= now).Select(x => x.Key).ToList())
                    addresses.Remove(expired);
            }

            foreach (var host in this.addressRecords.Where(x => x.Value.Count == 0).Select(x => x.Key).ToList())
                this.addressRecords.Remove(host);

            foreach (var state in this.instances.Values.Where(x => x.PtrExpiry <= now).ToList())
            {
                this.instances.Remove(state.FullName);
                this.srvRecords.Remove(state.FullName);
                this.txtRecords.Remove(state.FullName);

                if (state.Emitted)
                    this.Enqueue(new MdnsBrowseResult(MdnsBrowseStatus.Lost, this.BuildMinimal(state)));
            }

            var aged = Expire(this.srvRecords, now).Concat(Expire(this.txtRecords, now)).Distinct().ToList();
            foreach (var state in aged.Where(this.instances.ContainsKey).Select(x => this.instances[x]))
            {
                state.RequeryPending = config.ResolveServices;
                state.HoldUntil = config.ResolveServices ? now + config.ResolveTimeout : DateTimeOffset.MinValue;
            }

            this.Emit(now);
        }
    }


    /// <summary>
    /// Emits Found for every instance that is now resolved, or whose resolve deadline passed.
    /// Callers must hold <see cref="sync"/>.
    /// </summary>
    void Emit(DateTimeOffset now)
    {
        foreach (var state in this.instances.Values)
        {
            var service = this.Build(state);
            var complete = !config.ResolveServices || service.IsResolved;

            // an SRV/TXT that expired under a live PTR is being asked for again - keep what was
            // reported rather than flicker to a half-empty service, unless the answer never comes
            var held = state.Emitted &&
                       now < state.HoldUntil &&
                       (!this.srvRecords.ContainsKey(state.FullName) || !this.txtRecords.ContainsKey(state.FullName));

            if (!held && (complete || now >= state.ResolveDeadline))
            {
                var signature = Signature(service);
                if (!state.Emitted || state.Signature != signature)
                {
                    state.Emitted = true;
                    state.Signature = signature;
                    this.Enqueue(new MdnsBrowseResult(MdnsBrowseStatus.Found, service));
                }
            }
        }
    }


    MdnsService Build(InstanceState state)
    {
        var srv = this.srvRecords.GetValueOrDefault(state.FullName)?.Record;
        var txt = this.txtRecords.GetValueOrDefault(state.FullName)?.Record;

        var addresses = srv != null && this.addressRecords.TryGetValue(srv.Target, out var known)
            ? known.Keys.ToList()
            : new List<IPAddress>();

        return new MdnsService
        {
            InstanceName = state.InstanceName,
            ServiceType = serviceType.ServiceType,
            Domain = serviceType.Domain,
            HostName = srv?.Target.ToHostString(),
            Port = srv?.Port ?? 0,
            Addresses = addresses,
            TxtRecords = txt?.Values ?? MdnsConstants.EmptyTxt
        };
    }


    MdnsService BuildMinimal(InstanceState state) => new()
    {
        InstanceName = state.InstanceName,
        ServiceType = serviceType.ServiceType,
        Domain = serviceType.Domain
    };


    /// <summary>
    /// A cheap value identity for a resolved service so re-announcements only re-emit when
    /// something actually changed.
    /// </summary>
    static string Signature(MdnsService service)
        => String.Join(
            '|',
            service.HostName,
            service.Port,
            String.Join(',', service.Addresses.Select(x => x.ToString()).Order()),
            String.Join(',', service.TxtRecords.Select(x => $"{x.Key}={x.Value}").Order())
        );


    /// <summary>Removes the records whose TTL ran out and returns their names.</summary>
    static List<DnsName> Expire<T>(Dictionary<DnsName, CachedRecord<T>> records, DateTimeOffset now) where T : DnsResourceRecord
    {
        var expired = records.Where(x => x.Value.Expires <= now).Select(x => x.Key).ToList();
        foreach (var name in expired)
            records.Remove(name);

        return expired;
    }


    void Enqueue(MdnsBrowseResult result)
    {
        if (!this.channel.Writer.TryWrite(result))
            logger.LogWarning("Dropped an mDNS browse result for {Service} - the consumer is not reading", result.Service.FullName);
    }


    sealed class InstanceState
    {
        public required DnsName FullName { get; init; }

        public required string InstanceName { get; init; }

        /// <summary>When the PTR record ages out and the instance is considered gone.</summary>
        public DateTimeOffset PtrExpiry { get; set; }

        /// <summary>When we stop waiting for SRV/TXT/A and emit whatever we have.</summary>
        public required DateTimeOffset ResolveDeadline { get; init; }

        public bool Emitted { get; set; }

        public string? Signature { get; set; }

        /// <summary>Set when a cached SRV/TXT expired under a live PTR - the next resolve query asks once more.</summary>
        public bool RequeryPending { get; set; }

        /// <summary>Until when a re-asked instance keeps its last reported details while SRV/TXT are missing.</summary>
        public DateTimeOffset HoldUntil { get; set; } = DateTimeOffset.MinValue;
    }


    /// <summary>
    /// A cached SRV or TXT with its expiry and the RFC 6762 section 5.2 refresh schedule - questions
    /// at 80%, 85%, 90% and 95% of the TTL, each with the same 0-2% jitter so a link full of browsers
    /// does not ask in step.
    /// </summary>
    sealed class CachedRecord<T> where T : DnsResourceRecord
    {
        static readonly double[] refreshPoints = [0.80, 0.85, 0.90, 0.95];

        readonly DateTimeOffset received;
        readonly double jitter = Random.Shared.NextDouble() * 0.02;
        int refreshStep;

        public CachedRecord(T record, DateTimeOffset received)
        {
            this.Record = record;
            this.received = received;
            this.Expires = received + TimeSpan.FromSeconds(record.Ttl);
        }

        public T Record { get; }

        public DateTimeOffset Expires { get; }

        /// <summary>
        /// True once per refresh point reached. Points that passed while nobody looked are skipped
        /// together, so a stalled loop sends one question rather than four.
        /// </summary>
        public bool TakeRefresh(DateTimeOffset now)
        {
            var due = false;
            while (this.refreshStep < refreshPoints.Length && now >= this.PointAt(this.refreshStep))
            {
                this.refreshStep++;
                due = true;
            }
            return due;
        }

        DateTimeOffset PointAt(int step)
            => this.received + TimeSpan.FromSeconds(this.Record.Ttl * (refreshPoints[step] + this.jitter));
    }
}
