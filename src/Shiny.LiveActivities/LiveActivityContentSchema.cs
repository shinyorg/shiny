using System.Buffers;
using System.Text.Json;

namespace Shiny.LiveActivities;


/// <summary>
/// Serializes <see cref="LiveActivityContent"/> to the JSON shape both platforms — and a server push —
/// agree on.
/// </summary>
/// <remarks>
/// This is the contract between three pieces of code: this library, the Swift
/// <c>ShinyActivityAttributes.ContentState</c> a widget renders, and the <c>content-state</c> a server
/// sends over APNs. It is public so a server payload can be built (or verified) against exactly the same
/// shape the app produces.
/// <code>
/// {
///   "title":        "Out for delivery",   // string?
///   "body":         "2 stops away",       // string?
///   "shortStatus":  "5 min",              // string?
///   "progress":     0.65,                 // double?  0.0 - 1.0
///   "progressStart": 774835200.0,         // double?  seconds since 2001-01-01 (Swift's Date encoding)
///   "progressEnd":   774838800.0,         // double?
///   "indeterminate": false,               // bool
///   "data": { "orderId": "A-1234" }       // [String: String]
/// }
/// </code>
/// Dates use Swift's reference date (2001-01-01), not the Unix epoch, because ActivityKit decodes the
/// state with a stock <c>JSONDecoder</c>.
/// </remarks>
public static class LiveActivityContentSchema
{
    /// <summary>Seconds between the Unix epoch and Swift's reference date (2001-01-01T00:00:00Z).</summary>
    public const double AppleReferenceEpochOffset = 978307200d;


    /// <summary>Converts a date to the seconds-since-2001 value Swift's <c>Codable</c> expects.</summary>
    /// <param name="value">The date to convert.</param>
    public static double ToAppleReferenceSeconds(DateTimeOffset value)
        => (value.ToUnixTimeMilliseconds() / 1000d) - AppleReferenceEpochOffset;


    /// <summary>Serializes content to the shared content-state JSON.</summary>
    /// <param name="content">The content to serialize.</param>
    public static string ToJson(LiveActivityContent content)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();

            if (content.Title is not null) w.WriteString("title", content.Title);
            if (content.Body is not null) w.WriteString("body", content.Body);
            if (content.ShortStatus is not null) w.WriteString("shortStatus", content.ShortStatus);

            if (content.Progress is { } progress)
            {
                if (progress.Value is { } value)
                    w.WriteNumber("progress", value);

                if (progress.Start is { } start)
                    w.WriteNumber("progressStart", ToAppleReferenceSeconds(start));

                if (progress.End is { } end)
                    w.WriteNumber("progressEnd", ToAppleReferenceSeconds(end));

                if (progress.Indeterminate)
                    w.WriteBoolean("indeterminate", true);
            }

            w.WritePropertyName("data");
            w.WriteStartObject();
            foreach (var kvp in content.Data)
                w.WriteString(kvp.Key, kvp.Value);
            w.WriteEndObject();

            w.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }


    /// <summary>Serializes the static attributes of a start request.</summary>
    /// <param name="request">The request whose attributes to serialize.</param>
    public static string AttributesToJson(LiveActivityRequest request)
    {
        var buffer = new ArrayBufferWriter<byte>(128);
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            if (request.Kind is not null)
                w.WriteString("kind", request.Kind);

            w.WritePropertyName("values");
            w.WriteStartObject();
            foreach (var kvp in request.Attributes)
                w.WriteString(kvp.Key, kvp.Value);
            w.WriteEndObject();

            w.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
