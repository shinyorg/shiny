using System.Text;
using Shiny.Net.Discovery;
using Shiny.Net.Discovery.Internals;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class TxtRecordCodecTests
{
    [Fact]
    public void RoundTrips()
    {
        var source = new Dictionary<string, string>
        {
            ["version"] = "1.2.3",
            ["path"] = "/api/v1",
            ["unicode"] = "café ⚡"
        };

        var decoded = TxtRecordCodec.Decode(TxtRecordCodec.Encode(source));

        Assert.Equal(3, decoded.Count);
        Assert.Equal("1.2.3", decoded["version"]);
        Assert.Equal("/api/v1", decoded["path"]);
        Assert.Equal("café ⚡", decoded["unicode"]);
    }


    [Fact]
    public void KeysAreCaseInsensitive()
    {
        var decoded = TxtRecordCodec.Decode(TxtRecordCodec.Encode(new Dictionary<string, string> { ["Version"] = "1" }));
        Assert.Equal("1", decoded["VERSION"]);
    }


    [Fact]
    public void EmptyMapEncodesToTheSingleZeroLengthString()
    {
        // RFC 6763 section 6.1 - TXT RDATA is never allowed to be zero bytes
        Assert.Equal([0], TxtRecordCodec.Encode(null));
        Assert.Equal([0], TxtRecordCodec.Encode(new Dictionary<string, string>()));
    }


    [Fact]
    public void ValuesMayContainEqualsSigns()
    {
        var decoded = TxtRecordCodec.Decode(TxtRecordCodec.Encode(new Dictionary<string, string> { ["q"] = "a=b=c" }));
        Assert.Equal("a=b=c", decoded["q"]);
    }


    [Fact]
    public void KeyWithNoEqualsDecodesToAnEmptyValue()
    {
        var raw = Encode("flagonly");
        var decoded = TxtRecordCodec.Decode(raw);

        Assert.True(decoded.ContainsKey("flagonly"));
        Assert.Equal(String.Empty, decoded["flagonly"]);
    }


    [Fact]
    public void DuplicateKeysKeepTheFirst()
    {
        // RFC 6763 section 6.4
        var decoded = TxtRecordCodec.Decode(Encode("k=first", "k=second"));
        Assert.Equal("first", decoded["k"]);
    }


    [Fact]
    public void LeadingEqualsIsSkipped()
    {
        var decoded = TxtRecordCodec.Decode(Encode("=orphan", "good=1"));

        Assert.Single(decoded);
        Assert.Equal("1", decoded["good"]);
    }


    [Fact]
    public void TruncatedDataKeepsWhatWasReadable()
    {
        var good = Encode("a=1");
        var truncated = good.Concat(new byte[] { 40, (byte)'b' }).ToArray(); // claims 40 bytes, supplies 1

        var decoded = TxtRecordCodec.Decode(truncated);

        Assert.Single(decoded);
        Assert.Equal("1", decoded["a"]);
    }


    [Fact]
    public void RejectsAnEqualsSignInAKey()
        => Assert.Throws<MdnsException>(() => TxtRecordCodec.Encode(new Dictionary<string, string> { ["a=b"] = "1" }));


    [Fact]
    public void RejectsAnEmptyKey()
        => Assert.Throws<MdnsException>(() => TxtRecordCodec.Encode(new Dictionary<string, string> { [""] = "1" }));


    [Fact]
    public void RejectsAnEntryOver255Bytes()
    {
        var big = new Dictionary<string, string> { ["k"] = new string('x', 255) };
        var ex = Assert.Throws<MdnsException>(() => TxtRecordCodec.Encode(big));
        Assert.Contains("257 bytes", ex.Message);
    }


    static byte[] Encode(params string[] entries)
    {
        var buffer = new List<byte>();
        foreach (var entry in entries)
        {
            var bytes = Encoding.UTF8.GetBytes(entry);
            buffer.Add((byte)bytes.Length);
            buffer.AddRange(bytes);
        }
        return buffer.ToArray();
    }
}
