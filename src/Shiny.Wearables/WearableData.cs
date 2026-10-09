using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Shiny.Wearables;


/// <summary>
/// Turns text and key/values into the bytes the wearable API carries, and back. Text is UTF-8; key/values are a
/// JSON object. Both are plain bytes on the wire, so a Swift or Kotlin companion reads them with its own JSON parser.
/// <para>
/// No reflection is involved, so this is trim and AOT safe. Values may be <c>null</c>, strings, booleans, numbers,
/// <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, <see cref="TimeSpan"/>, <see cref="Guid"/>, <see cref="Uri"/>,
/// enums (written by name), <c>byte[]</c> (base64), <see cref="JsonElement"/>, nested dictionaries with string keys,
/// and lists or arrays of any of these.
/// </para>
/// </summary>
public static class WearableData
{
    /// <summary>Plain text as UTF-8 bytes. Structured data goes through <see cref="FromValues"/>.</summary>
    public static byte[] FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Encoding.UTF8.GetBytes(value);
    }


    /// <summary>Key/values as the UTF-8 bytes of a JSON object.</summary>
    /// <exception cref="NotSupportedException">A value is of a type listed nowhere in <see cref="WearableData"/>.</exception>
    public static byte[] FromValues<TValue>(IReadOnlyDictionary<string, TValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach (var pair in values)
            {
                writer.WritePropertyName(pair.Key);
                WriteValue(writer, pair.Value);
            }
            writer.WriteEndObject();
        }
        return buffer.ToArray();
    }


    /// <summary>Bytes as a UTF-8 string; empty when there are none.</summary>
    public static string GetString(byte[]? data)
        => data == null || data.Length == 0 ? String.Empty : Encoding.UTF8.GetString(data);


    /// <summary>
    /// The key/values of a JSON object. Empty when there are no bytes, so a companion that replied with nothing reads
    /// as no values.
    /// </summary>
    /// <exception cref="JsonException">The bytes are not a JSON object.</exception>
    public static IReadOnlyDictionary<string, JsonElement> GetValues(byte[]? data)
    {
        var result = new Dictionary<string, JsonElement>();
        if (data == null || data.Length == 0)
            return result;

        using var doc = JsonDocument.Parse(data);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException($"Expected a JSON object but the data is a JSON {doc.RootElement.ValueKind}.");

        foreach (var property in doc.RootElement.EnumerateObject())
            result[property.Name] = property.Value.Clone();

        return result;
    }


    static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:                      writer.WriteNullValue(); break;
            case string s:                  writer.WriteStringValue(s); break;
            case char c:                    writer.WriteStringValue(c.ToString()); break;
            case bool b:                    writer.WriteBooleanValue(b); break;
            case byte n:                    writer.WriteNumberValue(n); break;
            case sbyte n:                   writer.WriteNumberValue(n); break;
            case short n:                   writer.WriteNumberValue(n); break;
            case ushort n:                  writer.WriteNumberValue(n); break;
            case int n:                     writer.WriteNumberValue(n); break;
            case uint n:                    writer.WriteNumberValue(n); break;
            case long n:                    writer.WriteNumberValue(n); break;
            case ulong n:                   writer.WriteNumberValue(n); break;
            case float n:                   writer.WriteNumberValue(n); break;
            case double n:                  writer.WriteNumberValue(n); break;
            case decimal n:                 writer.WriteNumberValue(n); break;
            case DateTime dt:               writer.WriteStringValue(dt); break;
            case DateTimeOffset dto:        writer.WriteStringValue(dto); break;
            case TimeSpan ts:               writer.WriteStringValue(ts.ToString("c", CultureInfo.InvariantCulture)); break;
            case Guid g:                    writer.WriteStringValue(g); break;
            case Uri u:                     writer.WriteStringValue(u.OriginalString); break;
            case Enum e:                    writer.WriteStringValue(e.ToString()); break;
            case byte[] bytes:              writer.WriteBase64StringValue(bytes); break;
            case JsonElement element:       element.WriteTo(writer); break;
            case JsonDocument document:     document.WriteTo(writer); break;

            case IDictionary dictionary:
                writer.WriteStartObject();
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is not string key)
                        throw new NotSupportedException($"Dictionary keys must be strings; found {entry.Key.GetType().FullName}.");

                    writer.WritePropertyName(key);
                    WriteValue(writer, entry.Value);
                }
                writer.WriteEndObject();
                break;

            case IEnumerable list:
                writer.WriteStartArray();
                foreach (var item in list)
                    WriteValue(writer, item);
                writer.WriteEndArray();
                break;

            default:
                throw new NotSupportedException(
                    $"{value.GetType().FullName} cannot be written as a wearable value. Use a string, number, boolean, date, Guid, enum, byte[], JsonElement, or a nested dictionary or list of these."
                );
        }
    }
}
