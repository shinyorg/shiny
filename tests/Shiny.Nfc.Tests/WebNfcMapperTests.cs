using System.Text;
using Shiny.Nfc.Infrastructure;
using Xunit;

namespace Shiny.Nfc.Tests;


/// <summary>
/// Web NFC unpacks well-known records itself, so these check the round trip through the shapes
/// Chrome actually hands over and accepts - not just NDEF bytes in and out.
/// </summary>
public class WebNfcMapperTests
{
    static string B64(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));


    [Fact]
    public void Reads_what_chrome_delivers()
    {
        var json = $$"""
        [
          { "recordType": "url", "data": "{{B64("https://shinylib.net/client/nfc")}}" },
          { "recordType": "text", "data": "{{B64("bonjour")}}", "lang": "fr", "encoding": "utf-8" },
          { "recordType": "mime", "mediaType": "application/json", "data": "{{B64("{}")}}" },
          { "recordType": "android.com:pkg", "data": "{{B64("org.shiny.app")}}" },
          { "recordType": "unknown", "data": "AQI=" }
        ]
        """;

        var message = WebNfcMapper.FromJson(json)!;

        Assert.True(message.Records[0].TryGetUri(out var uri));
        Assert.Equal("https://shinylib.net/client/nfc", uri);
        Assert.Equal(NdefRecord.CreateUri("https://shinylib.net/client/nfc"), message.Records[0]);

        Assert.True(message.Records[1].TryGetText(out var text));
        Assert.Equal(new NdefText("bonjour", "fr"), text);

        Assert.Equal("application/json", message.Records[2].MimeType);
        Assert.Equal(NdefRecord.CreateAndroidApplication("org.shiny.app"), message.Records[3]);
        Assert.Equal(NdefTypeNameFormat.Unknown, message.Records[4].TypeNameFormat);
        Assert.Equal(new byte[] { 1, 2 }, message.Records[4].Payload.ToArray());
    }


    [Fact]
    public void A_tag_with_no_records_reads_as_no_message()
    {
        Assert.Null(WebNfcMapper.FromJson("[]"));
        Assert.Null(WebNfcMapper.FromJson("""[{ "recordType": "empty" }]"""));
    }


    [Fact]
    public void Writes_well_known_records_as_web_nfc_wants_them()
    {
        var web = WebNfcMapper.ToWeb(NdefRecord.CreateUri("https://shinylib.net"), false);
        Assert.Equal(new WebNfcRecord("url", Text: "https://shinylib.net"), web);

        web = WebNfcMapper.ToWeb(NdefRecord.CreateText("hello", "en"), false);
        Assert.Equal(new WebNfcRecord("text", Text: "hello", Lang: "en", Encoding: "utf-8"), web);

        web = WebNfcMapper.ToWeb(NdefRecord.CreateMime("text/plain", "hi"u8.ToArray()), false);
        Assert.Equal("mime", web.RecordType);
        Assert.Equal("text/plain", web.MediaType);
        Assert.Equal(Convert.ToBase64String("hi"u8), web.Data);

        web = WebNfcMapper.ToWeb(NdefRecord.CreateAbsoluteUri("https://example.com/x"), false);
        Assert.Equal(new WebNfcRecord("absolute-url", Text: "https://example.com/x"), web);
    }


    [Fact]
    public void A_smart_poster_is_written_as_nested_records()
    {
        var web = WebNfcMapper.ToWeb(NdefRecord.CreateSmartPoster("https://shinylib.net", "Shiny"), false);

        Assert.Equal("smart-poster", web.RecordType);
        Assert.NotNull(web.Records);
        Assert.Contains(web.Records, r => r is { RecordType: "url", Text: "https://shinylib.net" });
        Assert.Contains(web.Records, r => r is { RecordType: "text", Text: "Shiny" });
    }


    [Fact]
    public void A_smart_poster_read_keeps_its_payload()
    {
        var poster = NdefRecord.CreateSmartPoster("https://shinylib.net", "Shiny");
        var json = $$"""[{ "recordType": "smart-poster", "data": "{{Convert.ToBase64String(poster.Payload.Span)}}" }]""";

        var read = WebNfcMapper.FromJson(json)!.Records[0];

        Assert.Equal(poster, read);
        Assert.True(read.TryGetSmartPoster(out var parsed));
        Assert.Equal("https://shinylib.net", parsed.Uri);
    }


    [Fact]
    public void Other_well_known_types_cannot_be_written_at_the_top_level()
    {
        var handover = new NdefRecord(NdefTypeNameFormat.WellKnown, "Hr"u8.ToArray(), new byte[] { 0x12 });
        Assert.Throws<NfcNotSupportedException>(() => WebNfcMapper.ToJson(new NdefMessage(handover)));
    }


    [Fact]
    public void Json_round_trips_through_the_generated_context()
    {
        var message = new NdefMessage(NdefRecord.CreateUri("https://shinylib.net"), NdefRecord.CreateText("hello"));
        var json = WebNfcMapper.ToJson(message);

        Assert.Contains("\"recordType\":\"url\"", json);
        Assert.DoesNotContain("\"mediaType\"", json);
    }
}
