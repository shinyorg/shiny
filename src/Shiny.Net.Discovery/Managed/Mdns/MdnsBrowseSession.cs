using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Internals;
using Shiny.Net.Discovery.Managed.Dns;

namespace Shiny.Net.Discovery.Managed;


/// <summary>
/// One live browse. Sends the PTR query on a backoff, feeds every received record into a small
/// TTL-aware cache, and pushes appear/disappear changes onto a channel.
/// </summary>
sealed class MdnsBrowseSession(
    MdnsMulticastClient client,
    ServiceTypeInfo serviceType,
    MdnsBrowseConfig config,
    ILogger logger
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
    readonly Dictionary<DnsName, SrvRecord> srvRecords = new();
    readonly Dictionary<DnsName, TxtRecord> txtRecords = new();
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
            await foreach (var result in this.channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
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
        var nextQuery = DateTimeOffset.UtcNow;
        var queryInterval = initialQueryInterval;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var now = DateTimeOffset.UtcNow;
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

                await Task.Delay(tickInterval, ct).ConfigureAwait(false);
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


    /// <summary>
    /// Asks for the SRV/TXT of every instance we know about but have not resolved, and the
    /// A/AAAA of every SRV target we have no address for.
    /// </summary>
    async Task SendResolveQueries(CancellationToken ct)
    {
        if (!config.ResolveServices)
            return;

        var query = DnsMessage.CreateQuery();
        lock (this.sync)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var instance in this.instances.Values)
            {
                // once the deadline passes we emit what we have and stop asking
                if (now <= instance.ResolveDeadline)
                {
                    if (!this.srvRecords.ContainsKey(instance.FullName))
                        query.Questions.Add(new DnsQuestion { Name = instance.FullName, Type = DnsRecordType.SRV });

                    if (!this.txtRecords.ContainsKey(instance.FullName))
                        query.Questions.Add(new DnsQuestion { Name = instance.FullName, Type = DnsRecordType.TXT });

                    if (this.srvRecords.TryGetValue(instance.FullName, out var srv) && !this.HasAddresses(srv.Target))
                    {
                        query.Questions.Add(new DnsQuestion { Name = srv.Target, Type = DnsRecordType.A });
                        query.Questions.Add(new DnsQuestion { Name = srv.Target, Type = DnsRecordType.AAAA });
                    }
                }
            }
        }

        if (query.Questions.Count > 0)
            await client.Send(query, ct).ConfigureAwait(false);
    }


    void OnMessageReceived(DnsMessage message, IPEndPoint from)
    {
        lock (this.sync)
        {
            foreach (var record in message.CacheableRecords)
                this.Apply(record);

            this.Emit(DateTimeOffset.UtcNow);
        }
    }


    void Apply(DnsResourceRecord record)
    {
        var now = DateTimeOffset.UtcNow;

        switch (record)
        {
            case PtrRecord ptr when ptr.Name.Equals(this.typeName):
                this.ApplyPtr(ptr, now);
                break;

            case SrvRecord srv:
                if (srv.IsGoodbye)
                    this.srvRecords.Remove(srv.Name);
                else
                    this.srvRecords[srv.Name] = srv;
                break;

            case TxtRecord txt:
                if (txt.IsGoodbye)
                    this.txtRecords.Remove(txt.Name);
                else
                    this.txtRecords[txt.Name] = txt;
                break;

            case AddressRecord address:
                this.ApplyAddress(address, now);
                break;
        }
    }


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
    /// Drops expired records and emits Lost for instances whose PTR aged out.
    /// </summary>
    void Sweep()
    {
        lock (this.sync)
        {
            var now = DateTimeOffset.UtcNow;

            foreach (var addresses in this.addressRecords.Values)
            {
                foreach (var expired in addresses.Where(x => x.Value <= now).Select(x => x.Key).ToList())
                    addresses.Remove(expired);
            }

            foreach (var state in this.instances.Values.Where(x => x.PtrExpiry <= now).ToList())
            {
                this.instances.Remove(state.FullName);
                this.srvRecords.Remove(state.FullName);
                this.txtRecords.Remove(state.FullName);

                if (state.Emitted)
                    this.Enqueue(new MdnsBrowseResult(MdnsBrowseStatus.Lost, this.BuildMinimal(state)));
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

            if (complete || now >= state.ResolveDeadline)
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
        this.srvRecords.TryGetValue(state.FullName, out var srv);
        this.txtRecords.TryGetValue(state.FullName, out var txt);

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
    }
}
