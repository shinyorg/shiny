using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.Nfc.Infrastructure;


/// <summary>
/// One Web NFC <c>NDEFRecord</c> as it crosses the JS boundary. Bytes travel as base64, a
/// string payload (text, URL) as <see cref="Text"/>, and a smart poster's inner records as
/// <see cref="Records"/>.
/// </summary>
sealed record WebNfcRecord(
    string RecordType,
    string? MediaType = null,
    string? Id = null,
    string? Data = null,
    string? Text = null,
    string? Lang = null,
    string? Encoding = null,
    WebNfcRecord[]? Records = null
);


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WebNfcRecord[]))]
partial class WebNfcJsonContext : JsonSerializerContext;


/// <summary>
/// Translates between <see cref="NdefRecord"/> and Web NFC's <c>NDEFRecord</c>, which unpacks the
/// well-known types itself: a URL record arrives as the expanded URL, a text record as the text with
/// its language and encoding alongside, and a well-known local type as <c>:type</c>. Compiled on every
/// target so the mapping is unit tested on net10.0; the browser manager only moves the JSON.
/// </summary>
static class WebNfcMapper
{
    public static NdefMessage? FromJson(string json)
    {
        var records = JsonSerializer.Deserialize(json, WebNfcJsonContext.Default.WebNfcRecordArray) ?? [];
        if (records.Length == 0)
            return null;

        var message = new NdefMessage(records.Select(FromWeb));
        return message.IsEmpty ? null : message;
    }


    public static string ToJson(NdefMessage message)
        => JsonSerializer.Serialize(message.Records.Select(r => ToWeb(r, false)).ToArray(), WebNfcJsonContext.Default.WebNfcRecordArray);


    public static NdefRecord FromWeb(WebNfcRecord r)
    {
        var data = r.Data == null ? [] : Convert.FromBase64String(r.Data);
        var id = r.Id == null ? default : Encoding.UTF8.GetBytes(r.Id);

        return r.RecordType switch
        {
            "empty" => NdefRecord.Empty,
            "text" => WithId(NdefRecord.CreateText(DecodeText(data, r.Encoding), r.Lang ?? "en", IsUtf16(r.Encoding)), id),
            "url" => WithId(NdefRecord.CreateUri(Encoding.UTF8.GetString(data)), id),
            "absolute-url" => new NdefRecord(NdefTypeNameFormat.AbsoluteUri, data, default, id),
            "mime" => new NdefRecord(NdefTypeNameFormat.Media, Ascii(r.MediaType ?? "application/octet-stream"), data, id),
            "smart-poster" => new NdefRecord(NdefTypeNameFormat.WellKnown, Ascii(NdefRecord.SmartPosterType), data, id),
            "unknown" => new NdefRecord(NdefTypeNameFormat.Unknown, default, data, id),
            var local when local.StartsWith(':') => new NdefRecord(NdefTypeNameFormat.WellKnown, Ascii(local[1..]), data, id),
            var external => new NdefRecord(NdefTypeNameFormat.External, Ascii(external), data, id)
        };
    }


    /// <param name="nested">Inside a smart poster, where Web NFC takes well-known local types as <c>:type</c>.</param>
    public static WebNfcRecord ToWeb(NdefRecord record, bool nested)
    {
        var id = record.Id.IsEmpty ? null : Encoding.UTF8.GetString(record.Id.Span);
        var data = record.Payload.IsEmpty ? null : Convert.ToBase64String(record.Payload.Span);

        switch (record.TypeNameFormat)
        {
            case NdefTypeNameFormat.Empty:
                return new("empty");

            case NdefTypeNameFormat.WellKnown when record.IsWellKnown(NdefRecord.TextType) && record.TryGetText(out var text):
                return new("text", Id: id, Text: text.Text, Lang: text.LanguageCode, Encoding: "utf-8");

            case NdefTypeNameFormat.WellKnown when record.IsWellKnown(NdefRecord.UriType) && record.TryGetUri(out var uri):
                return new("url", Id: id, Text: uri);

            case NdefTypeNameFormat.WellKnown when record.IsWellKnown(NdefRecord.SmartPosterType):
                var inner = NdefMessage.Parse(record.Payload.Span);
                return new("smart-poster", Id: id, Records: inner.Records.Select(x => ToWeb(x, true)).ToArray());

            case NdefTypeNameFormat.WellKnown when nested:
                return new(":" + record.TypeString, Id: id, Data: data);

            case NdefTypeNameFormat.WellKnown:
                throw new NfcNotSupportedException(
                    $"Web NFC can only write the text, URL and smart poster well-known types at the top level, not '{record.TypeString}'",
                    NfcCapabilities.WriteNdef
                );

            case NdefTypeNameFormat.Media:
                return new("mime", MediaType: record.TypeString, Id: id, Data: data);

            case NdefTypeNameFormat.AbsoluteUri:
                return new("absolute-url", Id: id, Text: record.TypeString);

            case NdefTypeNameFormat.External:
                return new(record.TypeString, Id: id, Data: data);

            default:
                return new("unknown", Id: id, Data: data);
        }
    }


    // the language code is already split off by Web NFC; the factory re-encodes the status byte
    static NdefRecord WithId(NdefRecord record, ReadOnlyMemory<byte> id)
        => id.IsEmpty ? record : new NdefRecord(record.TypeNameFormat, record.Type, record.Payload, id);

    static bool IsUtf16(string? encoding) => encoding?.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase) == true;

    static string DecodeText(byte[] data, string? encoding) => encoding?.ToLowerInvariant() switch
    {
        "utf-16" or "utf-16be" => Encoding.BigEndianUnicode.GetString(data),
        "utf-16le" => Encoding.Unicode.GetString(data),
        _ => Encoding.UTF8.GetString(data)
    };

    static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);
}
