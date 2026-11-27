using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shiny.Net.Discovery.Internals;
using Shiny.Net.Discovery.Managed;
using Shiny.Net.Discovery.Managed.Dns;
// .NET 11 added System.Net.PtrRecord/SrvRecord/TxtRecord/AddressRecord - these names mean the mDNS wire records
using AddressRecord = Shiny.Net.Discovery.Managed.Dns.AddressRecord;
using PtrRecord = Shiny.Net.Discovery.Managed.Dns.PtrRecord;
using SrvRecord = Shiny.Net.Discovery.Managed.Dns.SrvRecord;
using TxtRecord = Shiny.Net.Discovery.Managed.Dns.TxtRecord;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


/// <summary>
/// SRV and TXT expire by their own TTL (120 s on Apple responders) while the PTR lives far longer
/// (75 min), so a browse must refresh them, drop them when they age out, and ask again rather than
/// report the instance lost.
/// </summary>
public class MdnsBrowseSessionExpiryTests : IDisposable
{
    static readonly DnsName TypeName = DnsName.ForServiceType("_http", "_tcp", "local");
    static readonly DnsName Instance = DnsName.ForInstance("Living Room", "_http", "_tcp", "local");
    static readonly DnsName Host = DnsName.Parse("mybox.local");
    static readonly IPEndPoint From = new(IPAddress.Parse("192.168.1.20"), 5353);

    readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero));
    readonly MdnsMulticastClient client = new(NullLogger.Instance);
    readonly MdnsBrowseSession session;


    public MdnsBrowseSessionExpiryTests()
    {
        var config = new MdnsBrowseConfig("_http._tcp");
        this.session = new MdnsBrowseSession(
            this.client,
            ServiceTypeParser.Parse(config.ServiceType, config.Domain),
            config,
            NullLogger.Instance,
            this.clock
        );

        // the full announcement - long-lived PTR and address, short-lived SRV/TXT
        this.Receive(Ptr(4500), Srv(120), Txt(120), Address(4500));
        var found = Assert.Single(this.Drain());
        Assert.Equal(MdnsBrowseStatus.Found, found.Status);
        Assert.Equal(8080, found.Service.Port);

        // past the resolve timeout, so only the expiry and refresh paths ask anything
        this.clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Null(this.session.BuildResolveQuery());
    }


    [Fact]
    public void AnExpiredSrvUnderALivePtrIsAskedForAgainNotLost()
    {
        this.clock.Advance(TimeSpan.FromSeconds(115));
        this.session.Sweep();

        var query = this.session.BuildResolveQuery();
        Assert.NotNull(query);
        Assert.Contains(query.Questions, x => x.Type == DnsRecordType.SRV && x.Name.Equals(Instance));
        Assert.Contains(query.Questions, x => x.Type == DnsRecordType.TXT && x.Name.Equals(Instance));

        // one question, not one per tick
        Assert.Null(this.session.BuildResolveQuery());

        // nothing reported while the answer is awaited - neither lost nor a half-empty update
        Assert.Empty(this.Drain());

        // the responder answers and the same service carries on without a change to report
        this.Receive(Srv(120), Txt(120));
        Assert.Empty(this.Drain());
    }


    [Fact]
    public void AnExpiredSrvThatIsNeverAnsweredIsReportedAsItIs()
    {
        this.clock.Advance(TimeSpan.FromSeconds(115));
        this.session.Sweep();
        Assert.NotNull(this.session.BuildResolveQuery());

        // the hold lasts one resolve timeout, then the service is reported with what is left
        this.clock.Advance(TimeSpan.FromSeconds(6));
        this.session.Sweep();

        var update = Assert.Single(this.Drain());
        Assert.Equal(MdnsBrowseStatus.Found, update.Status);
        Assert.Equal(0, update.Service.Port);
    }


    [Fact]
    public void ARefreshedSrvIsKept()
    {
        // 80% of 120 s plus up to 2% jitter is at most 98.4 s after it was received
        this.clock.Advance(TimeSpan.FromSeconds(90));
        var refresh = this.session.BuildResolveQuery();
        Assert.NotNull(refresh);
        Assert.Contains(refresh.Questions, x => x.Type == DnsRecordType.SRV && x.Name.Equals(Instance));
        Assert.Contains(refresh.Questions, x => x.Type == DnsRecordType.TXT && x.Name.Equals(Instance));

        // once per refresh point
        Assert.Null(this.session.BuildResolveQuery());

        this.Receive(Srv(120), Txt(120));

        // well past the original expiry, still short of the refreshed record's 80% point
        this.clock.Advance(TimeSpan.FromSeconds(40));
        this.session.Sweep();

        Assert.Null(this.session.BuildResolveQuery());
        Assert.Empty(this.Drain());
    }


    [Fact]
    public void AGoodbyeRemovesTheSrvWithoutAskingAgain()
    {
        this.Receive(Srv(0));

        var update = Assert.Single(this.Drain());
        Assert.Equal(MdnsBrowseStatus.Found, update.Status);
        Assert.Equal(0, update.Service.Port);
        Assert.Null(update.Service.HostName);

        this.session.Sweep();
        Assert.Null(this.session.BuildResolveQuery());

        this.Receive(Ptr(0));
        Assert.Equal(MdnsBrowseStatus.Lost, Assert.Single(this.Drain()).Status);
    }


    void Receive(params DnsResourceRecord[] records)
    {
        var message = new DnsMessage { Flags = 0x8400 };
        message.Answers.AddRange(records);
        this.session.OnMessageReceived(message, From);
    }


    List<MdnsBrowseResult> Drain()
    {
        var results = new List<MdnsBrowseResult>();
        while (this.session.Results.TryRead(out var result))
            results.Add(result);

        return results;
    }


    static PtrRecord Ptr(uint ttl) => new() { Name = TypeName, Target = Instance, Ttl = ttl };

    static SrvRecord Srv(uint ttl) => new() { Name = Instance, Target = Host, Port = 8080, Ttl = ttl, CacheFlush = true };

    static TxtRecord Txt(uint ttl) => new()
    {
        Name = Instance,
        Data = TxtRecordCodec.Encode(new Dictionary<string, string> { ["path"] = "/api" }),
        Ttl = ttl,
        CacheFlush = true
    };

    static AddressRecord Address(uint ttl) => new() { Name = Host, Address = IPAddress.Parse("192.168.1.20"), Ttl = ttl, CacheFlush = true };


    public void Dispose() => this.client.Dispose();
}
