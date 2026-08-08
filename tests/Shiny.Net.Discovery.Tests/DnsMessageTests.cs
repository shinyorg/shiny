using System.Net;
using Shiny.Net.Discovery.Internals;
using Shiny.Net.Discovery.Managed.Dns;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class DnsMessageTests
{
    static readonly DnsName typeName = new("_http", "_tcp", "local");
    static readonly DnsName instanceName = new("Living Room", "_http", "_tcp", "local");
    static readonly DnsName hostName = new("mybox", "local");


    [Fact]
    public void QueryRoundTrips()
    {
        var query = DnsMessage.CreateQuery(0x1234);
        query.Questions.Add(new DnsQuestion { Name = typeName, Type = DnsRecordType.PTR });
        query.Questions.Add(new DnsQuestion { Name = instanceName, Type = DnsRecordType.ANY, UnicastResponse = true });

        var parsed = DnsMessage.Parse(query.ToArray());

        Assert.Equal(0x1234, parsed.Id);
        Assert.False(parsed.IsResponse);
        Assert.Equal(2, parsed.Questions.Count);

        Assert.Equal(typeName, parsed.Questions[0].Name);
        Assert.Equal(DnsRecordType.PTR, parsed.Questions[0].Type);
        Assert.False(parsed.Questions[0].UnicastResponse);

        Assert.Equal(instanceName, parsed.Questions[1].Name);
        Assert.True(parsed.Questions[1].UnicastResponse);
        Assert.Equal(DnsRecordClass.IN, parsed.Questions[1].Class);
    }


    [Fact]
    public void FullResponseRoundTrips()
    {
        var response = BuildFullResponse();
        var parsed = DnsMessage.Parse(response.ToArray());

        Assert.True(parsed.IsResponse);

        var ptr = Assert.IsType<PtrRecord>(parsed.Answers[0]);
        Assert.Equal(typeName, ptr.Name);
        Assert.Equal(instanceName, ptr.Target);
        Assert.Equal(4500u, ptr.Ttl);
        Assert.False(ptr.CacheFlush);

        var srv = Assert.IsType<SrvRecord>(parsed.Answers[1]);
        Assert.Equal(instanceName, srv.Name);
        Assert.Equal(hostName, srv.Target);
        Assert.Equal(8080, srv.Port);
        Assert.True(srv.CacheFlush);

        var txt = Assert.IsType<TxtRecord>(parsed.Answers[2]);
        Assert.Equal("1.0", txt.Values["version"]);

        var a = Assert.IsType<AddressRecord>(parsed.Answers[3]);
        Assert.Equal(IPAddress.Parse("192.168.1.50"), a.Address);
        Assert.Equal(DnsRecordType.A, a.Type);

        var aaaa = Assert.IsType<AddressRecord>(parsed.Answers[4]);
        Assert.Equal(IPAddress.Parse("fe80::1"), aaaa.Address);
        Assert.Equal(DnsRecordType.AAAA, aaaa.Type);
    }


    [Fact]
    public void InstanceNamesContainingDotsStayASingleLabel()
    {
        // "2.4Ghz" is one DNS-SD label even though it contains a dot - splitting a name on '.'
        // would silently corrupt it
        var dotted = new DnsName("Printer 2.4Ghz", "_http", "_tcp", "local");

        var response = DnsMessage.CreateResponse();
        response.Answers.Add(new PtrRecord { Name = typeName, Ttl = 120, Target = dotted });

        var parsed = DnsMessage.Parse(response.ToArray());
        var target = Assert.IsType<PtrRecord>(parsed.Answers[0]).Target;

        Assert.Equal(4, target.Count);
        Assert.Equal("Printer 2.4Ghz", target[0]);
        Assert.Equal(dotted, target);
    }


    [Fact]
    public void RepeatedSuffixesAreCompressed()
    {
        var response = BuildFullResponse();
        var bytes = response.ToArray();

        // "_http._tcp.local" appears in every record; without compression the packet would be
        // far larger than the sum of the unique names
        var uncompressed = "Living Room".Length + "_http._tcp.local".Length * 5;
        Assert.True(bytes.Length < 200, $"expected a compressed packet, got {bytes.Length} bytes");
        Assert.True(bytes.Length > uncompressed / 2);
    }


    [Fact]
    public void ReadsCompressionPointers()
    {
        // hand-built: header, then "_http._tcp.local" at offset 12, then a PTR whose target is
        // "x" + a pointer back to offset 12
        var packet = new List<byte>();
        packet.AddRange([0, 0, 0x84, 0, 0, 0, 0, 1, 0, 0, 0, 0]);

        // the record's own name doubles as the compression target
        var nameStart = packet.Count;
        AppendName(packet, "_http", "_tcp", "local");

        packet.AddRange([0, 12]);           // type PTR
        packet.AddRange([0, 1]);            // class IN
        packet.AddRange([0, 0, 0, 120]);    // ttl

        var rdata = new List<byte> { 1, (byte)'x', 0xC0, (byte)nameStart };
        packet.AddRange([0, (byte)rdata.Count]);
        packet.AddRange(rdata);

        var parsed = DnsMessage.Parse(packet.ToArray());
        var ptr = Assert.IsType<PtrRecord>(parsed.Answers[0]);

        Assert.Equal(new DnsName("x", "_http", "_tcp", "local"), ptr.Target);
    }


    [Fact]
    public void RejectsACompressionPointerLoop()
    {
        // a name at offset 12 that points straight back at itself
        var packet = new List<byte>();
        packet.AddRange([0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0]);  // one question
        packet.AddRange([0xC0, 12]);
        packet.AddRange([0, 12, 0, 1]);

        Assert.Throws<DnsFormatException>(() => DnsMessage.Parse(packet.ToArray()));
    }


    [Fact]
    public void RejectsATruncatedPacket()
        => Assert.Throws<DnsFormatException>(() => DnsMessage.Parse([0, 0, 0x84]));


    [Fact]
    public void OverstatedRecordCountsKeepWhatWasParsed()
    {
        var response = DnsMessage.CreateResponse();
        response.Answers.Add(new PtrRecord { Name = typeName, Ttl = 120, Target = instanceName });

        var bytes = response.ToArray();
        bytes[7] = 5; // claim five answers, supply one

        var parsed = DnsMessage.Parse(bytes);
        Assert.Single(parsed.Answers);
    }


    [Fact]
    public void UnknownRecordTypesSurviveWithoutDerailingTheRest()
    {
        var response = DnsMessage.CreateResponse();
        response.Answers.Add(new UnknownRecord
        {
            Name = hostName,
            Ttl = 120,
            RawType = DnsRecordType.NSEC,
            Data = [1, 2, 3, 4]
        });
        response.Answers.Add(new PtrRecord { Name = typeName, Ttl = 120, Target = instanceName });

        var parsed = DnsMessage.Parse(response.ToArray());

        Assert.Equal(2, parsed.Answers.Count);
        Assert.Equal(DnsRecordType.NSEC, parsed.Answers[0].Type);
        Assert.IsType<PtrRecord>(parsed.Answers[1]);
    }


    [Fact]
    public void CacheableRecordsExcludesTheAuthoritySection()
    {
        // a prober puts its *proposed* records in the authority section (RFC 6762 8.2). Caching
        // those makes a publisher that is about to be renamed briefly appear under the name it is
        // losing, so they must never reach a browse cache.
        var probe = DnsMessage.CreateQuery(0x4321);
        probe.Questions.Add(new DnsQuestion { Name = instanceName, Type = DnsRecordType.ANY, UnicastResponse = true });
        probe.Authorities.Add(new SrvRecord
        {
            Name = instanceName, Ttl = 120, CacheFlush = true, Port = 9999, Target = hostName
        });
        probe.Additionals.Add(new AddressRecord
        {
            Name = hostName, Ttl = 120, Address = IPAddress.Parse("10.0.0.1")
        });

        var parsed = DnsMessage.Parse(probe.ToArray());

        Assert.Single(parsed.Authorities);
        Assert.Single(parsed.CacheableRecords);
        Assert.IsType<AddressRecord>(parsed.CacheableRecords.Single());
        Assert.DoesNotContain(parsed.CacheableRecords, x => x is SrvRecord);
    }


    [Fact]
    public void ZeroTtlIsAGoodbye()
    {
        var response = DnsMessage.CreateResponse();
        response.Answers.Add(new PtrRecord { Name = typeName, Ttl = 0, Target = instanceName });

        var parsed = DnsMessage.Parse(response.ToArray());
        Assert.True(parsed.Answers[0].IsGoodbye);
    }


    [Fact]
    public void NamesCompareCaseInsensitively()
    {
        Assert.Equal(new DnsName("_HTTP", "_TCP", "LOCAL"), typeName);
        Assert.Equal(new DnsName("_HTTP", "_TCP", "LOCAL").GetHashCode(), typeName.GetHashCode());
        Assert.True(instanceName.EndsWith(new DnsName("_HTTP", "_tcp", "Local")));
        Assert.False(typeName.EndsWith(instanceName));
    }


    [Fact]
    public void ParseHonoursBackslashEscapes()
    {
        var parsed = DnsName.Parse("My\\.Printer._http._tcp.local");

        Assert.Equal(4, parsed.Count);
        Assert.Equal("My.Printer", parsed[0]);
    }


    static DnsMessage BuildFullResponse()
    {
        var response = DnsMessage.CreateResponse();
        response.Answers.Add(new PtrRecord { Name = typeName, Ttl = 4500, Target = instanceName });
        response.Answers.Add(new SrvRecord
        {
            Name = instanceName, Ttl = 120, CacheFlush = true, Port = 8080, Target = hostName
        });
        response.Answers.Add(new TxtRecord
        {
            Name = instanceName, Ttl = 4500, CacheFlush = true,
            Data = TxtRecordCodec.Encode(new Dictionary<string, string> { ["version"] = "1.0" })
        });
        response.Answers.Add(new AddressRecord
        {
            Name = hostName, Ttl = 120, CacheFlush = true, Address = IPAddress.Parse("192.168.1.50")
        });
        response.Answers.Add(new AddressRecord
        {
            Name = hostName, Ttl = 120, CacheFlush = true, Address = IPAddress.Parse("fe80::1")
        });
        return response;
    }


    static void AppendName(List<byte> buffer, params string[] labels)
    {
        foreach (var label in labels)
        {
            buffer.Add((byte)label.Length);
            buffer.AddRange(System.Text.Encoding.UTF8.GetBytes(label));
        }
        buffer.Add(0);
    }
}
