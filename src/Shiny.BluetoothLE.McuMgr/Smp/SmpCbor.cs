using System.Collections;
using System.Formats.Cbor;

namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// Converts SMP payloads between CBOR and plain .NET values. A payload is a map with string keys whose values are
/// <see cref="long"/> (or <see cref="ulong"/> above <see cref="long.MaxValue"/>), <see cref="string"/>,
/// <see cref="byte"/>[], <see cref="bool"/>, <see cref="double"/>, null, a list of values, or a nested map.
/// </summary>
public static class SmpCbor
{
    /// <summary>
    /// Encodes a payload map to CBOR.
    /// </summary>
    public static byte[] Encode(IReadOnlyDictionary<string, object?>? payload)
    {
        var writer = new CborWriter(CborConformanceMode.Lax);
        WriteMap(writer, payload ?? new Dictionary<string, object?>());
        return writer.Encode();
    }


    /// <summary>
    /// Decodes a CBOR map. Definite and indefinite length encodings are both accepted (older firmware built on
    /// tinycbor sends indefinite length maps). An empty payload decodes to an empty map.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> Decode(ReadOnlyMemory<byte> cbor)
    {
        if (cbor.IsEmpty)
            return new Dictionary<string, object?>();

        var reader = new CborReader(cbor, CborConformanceMode.Lax, allowMultipleRootLevelValues: true);
        if (reader.PeekState() != CborReaderState.StartMap)
            throw new InvalidDataException("The SMP payload is not a CBOR map");

        return ReadMap(reader);
    }


    static void WriteMap(CborWriter writer, IEnumerable<KeyValuePair<string, object?>> map)
    {
        var pairs = map.ToList();
        writer.WriteStartMap(pairs.Count);
        foreach (var pair in pairs)
        {
            writer.WriteTextString(pair.Key);
            WriteValue(writer, pair.Value);
        }
        writer.WriteEndMap();
    }


    static void WriteValue(CborWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNull();
                break;

            case bool b:
                writer.WriteBoolean(b);
                break;

            case string s:
                writer.WriteTextString(s);
                break;

            case byte[] bytes:
                writer.WriteByteString(bytes);
                break;

            case ReadOnlyMemory<byte> memory:
                writer.WriteByteString(memory.Span);
                break;

            case Memory<byte> memory:
                writer.WriteByteString(memory.Span);
                break;

            case byte or sbyte or short or ushort or int or long:
                writer.WriteInt64(Convert.ToInt64(value));
                break;

            case uint or ulong:
                writer.WriteUInt64(Convert.ToUInt64(value));
                break;

            case float f:
                writer.WriteSingle(f);
                break;

            case double d:
                writer.WriteDouble(d);
                break;

            case IEnumerable<KeyValuePair<string, object?>> map:
                WriteMap(writer, map);
                break;

            case IEnumerable list:
                var items = list.Cast<object?>().ToList();
                writer.WriteStartArray(items.Count);
                foreach (var item in items)
                    WriteValue(writer, item);
                writer.WriteEndArray();
                break;

            default:
                throw new ArgumentException($"{value.GetType()} cannot be encoded into an SMP payload");
        }
    }


    static Dictionary<string, object?> ReadMap(CborReader reader)
    {
        var map = new Dictionary<string, object?>();
        reader.ReadStartMap();
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.PeekState() is CborReaderState.TextString or CborReaderState.StartIndefiniteLengthTextString
                ? reader.ReadTextString()
                : ReadValue(reader)?.ToString() ?? String.Empty;

            map[key] = ReadValue(reader);
        }
        reader.ReadEndMap();
        return map;
    }


    static object? ReadValue(CborReader reader)
    {
        switch (reader.PeekState())
        {
            case CborReaderState.UnsignedInteger:
                var u = reader.ReadUInt64();
                return u <= long.MaxValue ? (long)u : u;

            case CborReaderState.NegativeInteger:
                return reader.ReadInt64();

            case CborReaderState.ByteString:
            case CborReaderState.StartIndefiniteLengthByteString:
                return reader.ReadByteString();

            case CborReaderState.TextString:
            case CborReaderState.StartIndefiniteLengthTextString:
                return reader.ReadTextString();

            case CborReaderState.StartArray:
                var list = new List<object?>();
                reader.ReadStartArray();
                while (reader.PeekState() != CborReaderState.EndArray)
                    list.Add(ReadValue(reader));
                reader.ReadEndArray();
                return list;

            case CborReaderState.StartMap:
                return ReadMap(reader);

            case CborReaderState.Boolean:
                return reader.ReadBoolean();

            case CborReaderState.Null:
                reader.ReadNull();
                return null;

            case CborReaderState.HalfPrecisionFloat:
            case CborReaderState.SinglePrecisionFloat:
            case CborReaderState.DoublePrecisionFloat:
                return reader.ReadDouble();

            case CborReaderState.Tag:
                reader.ReadTag();
                return ReadValue(reader);

            default:
                reader.SkipValue();
                return null;
        }
    }
}


/// <summary>
/// Typed accessors for decoded SMP payload maps.
/// </summary>
public static class SmpPayloadExtensions
{
    /// <summary>Returns the integer at <paramref name="key"/>, or null when it is missing or not an integer.</summary>
    public static long? GetInt64(this IReadOnlyDictionary<string, object?> payload, string key) => payload.TryGetValue(key, out var value)
        ? value switch
        {
            long l => l,
            ulong u when u <= long.MaxValue => (long)u,
            _ => null
        }
        : null;

    /// <summary>Returns the string at <paramref name="key"/>, or null.</summary>
    public static string? GetString(this IReadOnlyDictionary<string, object?> payload, string key)
        => payload.TryGetValue(key, out var value) ? value as string : null;

    /// <summary>Returns the byte string at <paramref name="key"/>, or null.</summary>
    public static byte[]? GetBytes(this IReadOnlyDictionary<string, object?> payload, string key)
        => payload.TryGetValue(key, out var value) ? value as byte[] : null;

    /// <summary>Returns the boolean at <paramref name="key"/>, or null.</summary>
    public static bool? GetBoolean(this IReadOnlyDictionary<string, object?> payload, string key)
        => payload.TryGetValue(key, out var value) && value is bool b ? b : null;

    /// <summary>Returns the list at <paramref name="key"/>, or an empty list.</summary>
    public static IReadOnlyList<object?> GetList(this IReadOnlyDictionary<string, object?> payload, string key)
        => payload.TryGetValue(key, out var value) && value is List<object?> list ? list : [];

    /// <summary>Returns the nested map at <paramref name="key"/>, or null.</summary>
    public static IReadOnlyDictionary<string, object?>? GetMap(this IReadOnlyDictionary<string, object?> payload, string key)
        => payload.TryGetValue(key, out var value) ? value as IReadOnlyDictionary<string, object?> : null;
}
