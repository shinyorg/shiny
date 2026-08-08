using System.Text;

namespace Shiny.Net.Discovery.Internals;


/// <summary>
/// Encodes and decodes DNS-SD TXT record key/value pairs per RFC 6763 section 6.
/// </summary>
static class TxtRecordCodec
{
    /// <summary>A single TXT string is length prefixed with one byte.</summary>
    public const int MaxStringLength = 255;

    static readonly byte[] empty = [0];


    /// <summary>
    /// Encodes key/value pairs into TXT RDATA. An empty or null map encodes to the single
    /// zero length string RFC 6763 requires - TXT RDATA is never allowed to be zero bytes.
    /// </summary>
    public static byte[] Encode(IReadOnlyDictionary<string, string>? values)
    {
        if (values == null || values.Count == 0)
            return empty;

        var buffer = new List<byte>(values.Count * 32);
        foreach (var pair in values)
        {
            if (String.IsNullOrEmpty(pair.Key))
                throw new MdnsException("TXT record keys cannot be empty.");

            if (pair.Key.Contains('='))
                throw new MdnsException($"The TXT record key '{pair.Key}' cannot contain '='.");

            // a null value means "key present, no value"; empty string means "key present, empty value"
            var entry = pair.Value == null
                ? pair.Key
                : $"{pair.Key}={pair.Value}";

            var bytes = Encoding.UTF8.GetBytes(entry);
            if (bytes.Length > MaxStringLength)
                throw new MdnsException($"The TXT record entry for '{pair.Key}' encodes to {bytes.Length} bytes - the limit is {MaxStringLength}.");

            buffer.Add((byte)bytes.Length);
            buffer.AddRange(bytes);
        }
        return buffer.ToArray();
    }


    /// <summary>
    /// Decodes TXT RDATA into key/value pairs. Malformed trailing bytes are ignored rather
    /// than thrown on - a peer with a bad TXT record should not break the whole browse.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length <= 1)
            return MdnsConstants.EmptyTxt;

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        while (index < data.Length)
        {
            var length = data[index++];
            if (length > 0)
            {
                if (index + length > data.Length)
                    break; // truncated - keep what we have

                var entry = Encoding.UTF8.GetString(data.Slice(index, length));
                index += length;

                var separator = entry.IndexOf('=');
                var key = separator < 0 ? entry : entry[..separator];
                var value = separator < 0 ? String.Empty : entry[(separator + 1)..];

                // a leading '=' is malformed per RFC 6763; on a duplicate key the first wins
                if (key.Length > 0)
                    result.TryAdd(key, value);
            }
        }

        return result.Count == 0 ? MdnsConstants.EmptyTxt : result;
    }
}
