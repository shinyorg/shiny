using System.Text;
using Xunit;

namespace Shiny.Nfc.Tests;


public class NdefMessageTests
{
    static byte[] Hex(string hex) => Convert.FromHexString(hex.Replace(" ", ""));


    [Fact]
    public void UriRecord_EncodesWithPrefixCode()
    {
        var bytes = new NdefMessage(NdefRecord.CreateUri("https://shinylib.net")).ToByteArray();

        // MB|ME|SR|well-known, type length 1, payload 13, 'U', 0x04 = "https://"
        Assert.Equal(Hex("D1 01 0D 55 04") .Concat(Encoding.ASCII.GetBytes("shinylib.net")), bytes);
    }


    [Fact]
    public void TextRecord_MatchesSpecificationLayout()
    {
        var bytes = new NdefMessage(NdefRecord.CreateText("Hello", "en")).ToByteArray();
        Assert.Equal(Hex("D1 01 08 54 02 65 6E 48 65 6C 6C 6F"), bytes);
    }


    [Fact]
    public void EmptyMessage_IsThreeBytes()
    {
        Assert.Equal(Hex("D0 00 00"), NdefMessage.Empty.ToByteArray());

        var parsed = NdefMessage.Parse(Hex("D0 00 00"));
        Assert.True(parsed.IsEmpty);
    }


    [Fact]
    public void MultipleRecords_SetBeginAndEndFlagsOnlyAtTheEdges()
    {
        var message = new NdefMessage(
            NdefRecord.CreateUri("https://a.io"),
            NdefRecord.CreateText("b"),
            NdefRecord.CreateMime("application/json", "{}"u8.ToArray())
        );
        var bytes = message.ToByteArray();

        Assert.Equal(0x91, bytes[0]);                    // MB|SR|well-known
        var second = 3 + 1 + message.Records[0].Payload.Length;
        Assert.Equal(0x11, bytes[second]);               // SR|well-known
        var third = second + 3 + 1 + message.Records[1].Payload.Length;
        Assert.Equal(0x52, bytes[third]);                // ME|SR|media

        Assert.Equal(message, NdefMessage.Parse(bytes));
        Assert.Equal(bytes.Length, message.Length);
    }


    [Fact]
    public void LongPayload_UsesFourByteLength()
    {
        var payload = new byte[300];
        Random.Shared.NextBytes(payload);
        var message = new NdefMessage(NdefRecord.CreateMime("application/octet-stream", payload));
        var bytes = message.ToByteArray();

        Assert.Equal(0, bytes[0] & 0x10);                // SR clear
        Assert.Equal(Hex("00 00 01 2C"), bytes[2..6]);
        Assert.Equal(message, NdefMessage.Parse(bytes));
    }


    [Fact]
    public void RecordId_SetsIdLengthFlag()
    {
        var record = new NdefRecord(NdefTypeNameFormat.WellKnown, "T"u8.ToArray(), Hex("02 65 6E 78"), "id1"u8.ToArray());
        var bytes = new NdefMessage(record).ToByteArray();

        Assert.Equal(0xD9, bytes[0]);                    // MB|ME|SR|IL|well-known
        Assert.Equal(Hex("D9 01 04 03 54 69 64 31 02 65 6E 78"), bytes);
        Assert.Equal(record, NdefMessage.Parse(bytes).Records.Single());
    }


    [Fact]
    public void ChunkedRecord_IsReassembled()
    {
        // first chunk: MB|CF|SR|media "text/plain" "Hel", middle: CF|SR|unchanged "lo ", last: ME|SR|unchanged "NFC"
        var bytes = Hex("B2 0A 03") .Concat("text/plain"u8.ToArray()).Concat("Hel"u8.ToArray())
            .Concat(Hex("36 00 03")).Concat("lo "u8.ToArray())
            .Concat(Hex("56 00 03")).Concat("NFC"u8.ToArray())
            .ToArray();

        var record = NdefMessage.Parse(bytes).Records.Single();
        Assert.Equal("text/plain", record.MimeType);
        Assert.Equal("Hello NFC", Encoding.ASCII.GetString(record.Payload.Span));
    }


    [Theory]
    [InlineData("", "No NDEF data")]
    [InlineData("11 01 01 54 00", "message-begin")]
    [InlineData("D1 01 05 54 02", "longer than the data")]
    [InlineData("D1 01 01 54 00 00", "follow the NDEF message-end")]
    [InlineData("91 01 01 54 00", "without a message-end")]
    [InlineData("D6 00 00", "Unchanged")]
    [InlineData("D7 00 00", "reserved")]
    [InlineData("D0 00 01 00", "empty NDEF record")]
    [InlineData("B2 01 01 61 00 52 00 00", "Unchanged type name format")]
    [InlineData("D1 00 00", "needs a type")]
    public void InvalidBytes_AreRejected(string hex, string expectedError)
    {
        var ex = Assert.Throws<NdefFormatException>(() => NdefMessage.Parse(Hex(hex)));
        Assert.Contains(expectedError, ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(NdefMessage.TryParse(Hex(hex), out _));
    }


    [Fact]
    public void NoRecords_IsRejected()
        => Assert.Throws<ArgumentException>(() => new NdefMessage());
}
