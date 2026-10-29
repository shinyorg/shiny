using System.Text;
using Xunit;

namespace Shiny.Nfc.Tests;


public class NdefRecordTests
{
    [Theory]
    [InlineData("https://www.shinylib.net", 0x02, "shinylib.net")]
    [InlineData("https://shinylib.net", 0x04, "shinylib.net")]
    [InlineData("http://www.x.io", 0x01, "x.io")]
    [InlineData("tel:+15551234", 0x05, "+15551234")]
    [InlineData("mailto:a@b.c", 0x06, "a@b.c")]
    [InlineData("ftp://anonymous:anonymous@host", 0x07, "host")]
    [InlineData("ftp://ftp.host", 0x08, "host")]
    [InlineData("urn:epc:id:sgtin:1", 0x1E, "sgtin:1")]
    [InlineData("urn:nfc:sn:x", 0x23, "sn:x")]
    [InlineData("myapp://open/1", 0x00, "myapp://open/1")]
    [InlineData("HTTPS://SHINYLIB.NET", 0x00, "HTTPS://SHINYLIB.NET")]
    public void Uri_UsesLongestPrefix_AndRoundTrips(string uri, byte expectedCode, string expectedRest)
    {
        var record = NdefRecord.CreateUri(uri);

        Assert.Equal(expectedCode, record.Payload.Span[0]);
        Assert.Equal(expectedRest, Encoding.UTF8.GetString(record.Payload.Span[1..]));
        Assert.True(record.TryGetUri(out var read));
        Assert.Equal(uri, read);
    }


    [Fact]
    public void Uri_NonAsciiIsUtf8()
    {
        var record = NdefRecord.CreateUri("https://例え.jp/パス");
        Assert.True(record.TryGetUri(out var read));
        Assert.Equal("https://例え.jp/パス", read);
    }


    [Fact]
    public void Uri_ReservedPrefixCode_IsNotAUri()
    {
        var record = new NdefRecord(NdefTypeNameFormat.WellKnown, "U"u8.ToArray(), new byte[] { 0x24, 0x61 });
        Assert.False(record.TryGetUri(out _));
    }


    [Fact]
    public void AbsoluteUri_ReadsFromTypeField()
    {
        var record = NdefRecord.CreateAbsoluteUri("https://shinylib.net/x");

        Assert.Equal(NdefTypeNameFormat.AbsoluteUri, record.TypeNameFormat);
        Assert.True(record.Payload.IsEmpty);
        Assert.True(record.TryGetUri(out var read));
        Assert.Equal("https://shinylib.net/x", read);
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Text_RoundTrips(bool utf16)
    {
        var record = NdefRecord.CreateText("Grüße, 世界", "de-CH", utf16);

        Assert.Equal(utf16 ? 0x85 : 0x05, record.Payload.Span[0]);
        Assert.True(record.TryGetText(out var text));
        Assert.Equal(new NdefText("Grüße, 世界", "de-CH"), text);
    }


    [Fact]
    public void Text_Utf16WithoutBom_IsBigEndian()
    {
        byte[] payload = [0x82, (byte)'e', (byte)'n', .. Encoding.BigEndianUnicode.GetBytes("Hi")];
        var record = new NdefRecord(NdefTypeNameFormat.WellKnown, "T"u8.ToArray(), payload);

        Assert.True(record.TryGetText(out var text));
        Assert.Equal("Hi", text.Text);
    }


    [Fact]
    public void Text_Utf16LittleEndianBom_IsHonoured()
    {
        byte[] payload = [0x82, (byte)'e', (byte)'n', 0xFF, 0xFE, .. Encoding.Unicode.GetBytes("Hi")];
        var record = new NdefRecord(NdefTypeNameFormat.WellKnown, "T"u8.ToArray(), payload);

        Assert.True(record.TryGetText(out var text));
        Assert.Equal("Hi", text.Text);
    }


    [Fact]
    public void Text_LanguageCodeLongerThanPayload_IsNotText()
    {
        var record = new NdefRecord(NdefTypeNameFormat.WellKnown, "T"u8.ToArray(), new byte[] { 0x05, (byte)'e' });
        Assert.False(record.TryGetText(out _));
    }


    [Fact]
    public void Text_LanguageCodeOver63_Throws()
        => Assert.Throws<ArgumentException>(() => NdefRecord.CreateText("x", new string('a', 64)));


    [Fact]
    public void Mime_IsLowercased()
    {
        var record = NdefRecord.CreateMime(" Application/JSON ", "{}"u8.ToArray());

        Assert.Equal(NdefTypeNameFormat.Media, record.TypeNameFormat);
        Assert.Equal("application/json", record.MimeType);
    }


    [Theory]
    [InlineData("json")]
    [InlineData("/json")]
    [InlineData("tëxt/plain")]
    public void Mime_Invalid_Throws(string mime)
        => Assert.Throws<ArgumentException>(() => NdefRecord.CreateMime(mime, default));


    [Fact]
    public void External_IsLowercasedDomainColonType()
    {
        var record = NdefRecord.CreateExternal("Shinylib.NET", "Asset", new byte[] { 1 });

        Assert.Equal(NdefTypeNameFormat.External, record.TypeNameFormat);
        Assert.Equal("shinylib.net:asset", record.TypeString);
    }


    [Fact]
    public void AndroidApplicationRecord()
    {
        var record = NdefRecord.CreateAndroidApplication("org.shiny.app");

        Assert.Equal(NdefRecord.AndroidApplicationType, record.TypeString);
        Assert.Equal("org.shiny.app", Encoding.UTF8.GetString(record.Payload.Span));
    }


    [Fact]
    public void SmartPoster_RoundTrips()
    {
        var record = NdefRecord.CreateSmartPoster("https://shinylib.net", "Shiny", "en");

        Assert.True(record.IsWellKnown(NdefRecord.SmartPosterType));
        Assert.True(record.TryGetSmartPoster(out var poster));
        Assert.Equal("https://shinylib.net", poster.Uri);
        Assert.Equal(new NdefText("Shiny", "en"), poster.Titles.Single());

        // a smart poster is not itself a URI record
        Assert.False(record.TryGetUri(out _));
    }


    [Fact]
    public void SmartPoster_WithoutUri_IsNotAPoster()
    {
        var payload = new NdefMessage(NdefRecord.CreateText("no uri")).ToByteArray();
        var record = new NdefRecord(NdefTypeNameFormat.WellKnown, "Sp"u8.ToArray(), payload);

        Assert.False(record.TryGetSmartPoster(out _));
    }


    [Fact]
    public void Constructor_EnforcesTypeNameFormatRules()
    {
        Assert.Throws<ArgumentException>(() => new NdefRecord(NdefTypeNameFormat.Empty, default, new byte[] { 1 }));
        Assert.Throws<ArgumentException>(() => new NdefRecord(NdefTypeNameFormat.Unknown, "x"u8.ToArray(), default));
        Assert.Throws<ArgumentException>(() => new NdefRecord(NdefTypeNameFormat.Media, default, default));
        Assert.Throws<ArgumentException>(() => new NdefRecord(NdefTypeNameFormat.WellKnown, new byte[256], default));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NdefRecord((NdefTypeNameFormat)6, default, default));
    }


    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(NdefRecord.CreateText("a"), NdefRecord.CreateText("a"));
        Assert.NotEqual(NdefRecord.CreateText("a"), NdefRecord.CreateText("b"));
        Assert.Equal(NdefRecord.CreateText("a").GetHashCode(), NdefRecord.CreateText("a").GetHashCode());
    }
}
