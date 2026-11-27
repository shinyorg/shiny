using System.Net;
using Shiny.Net.Discovery.Managed.Dns;
using Shiny.Net.Discovery.Managed.Multicast;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class DnsNameLimitTests
{
    /// <remarks>
    /// Ten one-byte labels followed by a pointer back to their start. Without a total length cap each
    /// jump re-reads the run, so a small packet decodes into a huge number of labels.
    /// </remarks>
    [Fact]
    public void ALabelLoopThroughAPointerIsRejected()
    {
        var packet = new List<byte> { 0, 0, 0x84, 0, 0, 0, 0, 1, 0, 0, 0, 0 };
        for (var i = 0; i < 10; i++)
            packet.AddRange([1, (byte)'a']);

        packet.AddRange([0xC0, 12]);              // back to the first label
        packet.AddRange([0, 1, 0, 1, 0, 0, 0, 120, 0, 0]); // type A, class IN, ttl, empty rdata

        Assert.Throws<DnsFormatException>(() => DnsMessage.Parse(packet.ToArray()));
    }


    [Fact]
    public void ANameAtTheLimitStillParses()
    {
        // 3 x (1 + 63) + (1 + 62) + 1 terminator = 256 would be over; 61 makes it exactly 255
        var name = new DnsName(new string('a', 63), new string('b', 63), new string('c', 63), new string('d', 61));
        var query = DnsMessage.CreateQuery();
        query.Questions.Add(new DnsQuestion { Name = name, Type = DnsRecordType.A });

        var parsed = DnsMessage.Parse(query.ToArray());
        Assert.Equal(name, parsed.Questions[0].Name);
    }


    [Fact]
    public void ANameOneByteOverTheLimitIsRejected()
    {
        var name = new DnsName(new string('a', 63), new string('b', 63), new string('c', 63), new string('d', 62));
        var query = DnsMessage.CreateQuery();
        query.Questions.Add(new DnsQuestion { Name = name, Type = DnsRecordType.A });

        Assert.Throws<DnsFormatException>(() => DnsMessage.Parse(query.ToArray()));
    }
}


public class MulticastRejoinTests
{
    static readonly IPAddress oldAddress = IPAddress.Parse("192.168.1.10");
    static readonly IPAddress newAddress = IPAddress.Parse("192.168.1.20");


    [Fact]
    public void TheSameAddressStaysJoined()
    {
        var previous = new[] { new MulticastSocketSet.V4Membership(oldAddress, 0) };
        Assert.True(MulticastSocketSet.WasJoinedV4(previous, oldAddress, 0));
    }


    /// <remarks>A DHCP change keeps the interface's membership, so the duplicate join is refused.</remarks>
    [Fact]
    public void ANewAddressOnAJoinedInterfaceStaysJoined()
    {
        var previous = new[] { new MulticastSocketSet.V4Membership(oldAddress, 4) };
        Assert.True(MulticastSocketSet.WasJoinedV4(previous, newAddress, 4));
    }


    [Fact]
    public void AnInterfaceThatWasNeverJoinedIsNot()
    {
        var previous = new[] { new MulticastSocketSet.V4Membership(oldAddress, 4) };
        Assert.False(MulticastSocketSet.WasJoinedV4(previous, newAddress, 5));
        Assert.False(MulticastSocketSet.WasJoinedV4(previous, newAddress, 0));
    }
}
