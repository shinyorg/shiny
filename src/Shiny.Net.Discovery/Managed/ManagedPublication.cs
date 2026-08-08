using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Internals;
using Shiny.Net.Discovery.Managed.Dns;

namespace Shiny.Net.Discovery.Managed;


/// <summary>
/// A live advertisement: probes for a free name, announces, then answers queries for the
/// service type, the instance and the host until it is disposed.
/// </summary>
sealed class ManagedPublication : IMdnsPublication
{
    /// <summary>RFC 6762 section 10 - address and SRV records get a short TTL so moves heal fast.</summary>
    const uint HostTtl = 120;

    /// <summary>PTR and TXT rarely change, so they get the long TTL.</summary>
    const uint SharedTtl = 4500;

    /// <summary>RFC 6762 section 8.1 - three probes, 250ms apart.</summary>
    const int ProbeCount = 3;

    const int MaxRenameAttempts = 10;

    static readonly TimeSpan probeInterval = TimeSpan.FromMilliseconds(250);
    static readonly TimeSpan announceInterval = TimeSpan.FromSeconds(1);

    readonly MulticastClient client;
    readonly ILogger logger;
    readonly IDisposable lease;
    readonly DnsName typeName;
    readonly DnsName hostName;
    readonly byte[] txtData;

    DnsName fullName;
    int disposed;


    ManagedPublication(
        MulticastClient client,
        ILogger logger,
        IDisposable lease,
        ServiceTypeInfo serviceType,
        string instanceName,
        DnsName hostName,
        int port,
        byte[] txtData
    )
    {
        this.client = client;
        this.logger = logger;
        this.lease = lease;
        this.hostName = hostName;
        this.txtData = txtData;

        this.InstanceName = instanceName;
        this.ServiceType = serviceType.ServiceType;
        this.Domain = serviceType.Domain;
        this.Port = port;

        this.typeName = DnsName.ForServiceType(serviceType.Application, serviceType.Protocol, serviceType.Domain);
        this.fullName = DnsName.ForInstance(instanceName, serviceType.Application, serviceType.Protocol, serviceType.Domain);
    }


    public string InstanceName { get; private set; }

    public string ServiceType { get; }

    public string Domain { get; }

    public int Port { get; }


    public static async Task<ManagedPublication> Create(
        MulticastClient client,
        ILogger logger,
        DnsName hostName,
        MdnsServiceRegistration registration,
        ServiceTypeInfo serviceType,
        CancellationToken ct
    )
    {
        var lease = client.Acquire();
        var publication = new ManagedPublication(
            client,
            logger,
            lease,
            serviceType,
            registration.InstanceName,
            hostName,
            registration.Port,
            TxtRecordCodec.Encode(registration.TxtRecords)
        );

        try
        {
            await publication.ClaimName(registration.InstanceName, serviceType, ct).ConfigureAwait(false);
            client.MessageReceived += publication.OnMessageReceived;
            await publication.Announce(ct).ConfigureAwait(false);
            return publication;
        }
        catch
        {
            await publication.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }


    /// <summary>
    /// Probes for the requested name, renaming "Name" -> "Name (2)" -> "Name (3)" until the
    /// link goes quiet, exactly like the platform responders do.
    /// </summary>
    async Task ClaimName(string requestedName, ServiceTypeInfo serviceType, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxRenameAttempts; attempt++)
        {
            var candidate = attempt == 1 ? requestedName : $"{requestedName} ({attempt})";
            this.InstanceName = candidate;
            this.fullName = DnsName.ForInstance(candidate, serviceType.Application, serviceType.Protocol, serviceType.Domain);

            if (await this.Probe(ct).ConfigureAwait(false))
            {
                if (attempt > 1)
                    this.logger.LogInformation("'{Requested}' was taken - publishing as '{Actual}' instead", requestedName, candidate);

                return;
            }
        }
        throw new MdnsException($"Could not find a free instance name for '{requestedName}.{serviceType.QualifiedName}' after {MaxRenameAttempts} attempts.");
    }


    /// <summary>
    /// Returns true when nobody on the link claims our proposed name.
    /// </summary>
    async Task<bool> Probe(CancellationToken ct)
    {
        var conflict = false;
        var probeName = this.fullName;

        // multicast loopback hands our own probe straight back to us, and its authority section
        // carries the very records we are claiming - without a marker we would always lose to
        // ourselves. mDNS ignores the transaction id, so it is free to use as one.
        var probeId = (ushort)Random.Shared.Next(1, ushort.MaxValue);

        void OnProbeReply(DnsMessage message, IPEndPoint from)
        {
            if (message.Id == probeId)
                return;

            // somebody answering for the name owns it, and somebody probing for it at the same
            // time as us is a tie we concede rather than fight
            var claimed = message.IsResponse
                ? message.Answers.Any(x => x.Name.Equals(probeName))
                : message.Authorities.Any(x => x.Name.Equals(probeName));

            if (claimed)
                Volatile.Write(ref conflict, true);
        }

        this.client.MessageReceived += OnProbeReply;
        try
        {
            for (var i = 0; i < ProbeCount; i++)
            {
                if (Volatile.Read(ref conflict))
                    return false;

                var probe = DnsMessage.CreateQuery(probeId);
                probe.Questions.Add(new DnsQuestion
                {
                    Name = probeName,
                    Type = DnsRecordType.ANY,
                    UnicastResponse = true
                });

                // RFC 6762 section 8.2 - the proposed records go in the authority section so a
                // simultaneous prober can break the tie without another round trip
                probe.Authorities.Add(this.BuildSrv(HostTtl));
                probe.Authorities.Add(this.BuildTxt(SharedTtl));

                await this.client.Send(probe, ct).ConfigureAwait(false);
                await Task.Delay(probeInterval, ct).ConfigureAwait(false);
            }
        }
        finally
        {
            this.client.MessageReceived -= OnProbeReply;
        }
        return !Volatile.Read(ref conflict);
    }


    async Task Announce(CancellationToken ct)
    {
        // RFC 6762 section 8.3 - announce at least twice, a second apart
        for (var i = 0; i < 2; i++)
        {
            await this.client.Send(this.BuildResponse(true, true, true, null), ct).ConfigureAwait(false);

            if (i == 0)
                await Task.Delay(announceInterval, ct).ConfigureAwait(false);
        }
    }


    void OnMessageReceived(DnsMessage message, IPEndPoint from)
    {
        if (message.IsResponse || Volatile.Read(ref this.disposed) == 1)
            return;

        var wantsType = message.Questions.Any(x =>
            (x.Type == DnsRecordType.PTR || x.Type == DnsRecordType.ANY) && x.Name.Equals(this.typeName));

        var wantsInstance = message.Questions.Any(x =>
            (x.Type == DnsRecordType.SRV || x.Type == DnsRecordType.TXT || x.Type == DnsRecordType.ANY) && x.Name.Equals(this.fullName));

        var wantsHost = message.Questions.Any(x =>
            (x.Type == DnsRecordType.A || x.Type == DnsRecordType.AAAA || x.Type == DnsRecordType.ANY) && x.Name.Equals(this.hostName));

        if (!wantsType && !wantsInstance && !wantsHost)
            return;

        // The QU (unicast response) bit is deliberately ignored. Probes set it, but every mDNS
        // socket on the machine binds 5353 with SO_REUSEPORT, and the kernel hands a unicast
        // datagram for a reused port to exactly one of those sockets - which may not be the
        // prober's. Defending a name by multicast reaches every prober, which is also what
        // RFC 6762 section 8.2 wants when a unique record is contested.
        _ = this.Respond(wantsType, wantsInstance, wantsHost);
    }


    async Task Respond(bool includePtr, bool includeInstance, bool includeHost)
    {
        try
        {
            // RFC 6762 section 6 - answers to a shared record (PTR) are delayed 20-120ms so a
            // link full of responders does not answer in lockstep. A contested unique record is
            // defended immediately.
            if (includePtr && !includeInstance)
                await Task.Delay(Random.Shared.Next(20, 120)).ConfigureAwait(false);

            // Re-check after the delay. Answering is fire-and-forget, so without this a reply
            // queued just before Dispose can be sent after the goodbye and resurrect the service
            // in every browser's cache until its TTL runs out.
            if (Volatile.Read(ref this.disposed) == 1)
                return;

            var response = this.BuildResponse(includePtr, includeInstance, includeHost, null);
            await this.client.Send(response).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Failed to answer an mDNS query for {FullName}", this.fullName);
        }
    }


    DnsMessage BuildResponse(bool includePtr, bool includeInstance, bool includeHost, uint? ttlOverride)
    {
        var response = DnsMessage.CreateResponse();

        if (includePtr)
            response.Answers.Add(this.BuildPtr(ttlOverride ?? SharedTtl));

        if (includePtr || includeInstance)
        {
            response.Answers.Add(this.BuildSrv(ttlOverride ?? HostTtl));
            response.Answers.Add(this.BuildTxt(ttlOverride ?? SharedTtl));
        }

        if (includePtr || includeInstance || includeHost)
        {
            foreach (var record in this.BuildAddresses(ttlOverride ?? HostTtl))
                response.Answers.Add(record);
        }
        return response;
    }


    // PTR is a shared record - several devices legitimately answer for the same service type,
    // so it must never carry the cache flush bit
    PtrRecord BuildPtr(uint ttl) => new()
    {
        Name = this.typeName,
        Ttl = ttl,
        Target = this.fullName
    };


    SrvRecord BuildSrv(uint ttl) => new()
    {
        Name = this.fullName,
        Ttl = ttl,
        CacheFlush = true,
        Port = (ushort)this.Port,
        Target = this.hostName
    };


    TxtRecord BuildTxt(uint ttl) => new()
    {
        Name = this.fullName,
        Ttl = ttl,
        CacheFlush = true,
        Data = this.txtData
    };


    IEnumerable<AddressRecord> BuildAddresses(uint ttl)
        => this.client
            .LocalAddresses
            .Where(x => x.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
            .Select(x => new AddressRecord
            {
                Name = this.hostName,
                Ttl = ttl,
                CacheFlush = true,
                Address = x
            });


    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) == 1)
            return;

        this.client.MessageReceived -= this.OnMessageReceived;
        try
        {
            // RFC 6762 section 10.1 - a goodbye is the same records with a zero TTL, sent twice
            var goodbye = this.BuildResponse(true, true, true, 0);
            await this.client.Send(goodbye, CancellationToken.None).ConfigureAwait(false);
            await Task.Delay(probeInterval).ConfigureAwait(false);
            await this.client.Send(goodbye, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Failed to send the mDNS goodbye for {FullName}", this.fullName);
        }
        finally
        {
            this.lease.Dispose();
        }
    }
}
