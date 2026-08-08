using System.Net;
using Foundation;

namespace Shiny.Net.Discovery;


static class AppleMdnsExtensions
{
    // Darwin sockaddr layout: sa_len, sa_family, then the family specific payload
    const byte AfInet = 2;
    const byte AfInet6 = 30;


    /// <summary>
    /// NSNetService hands back raw sockaddr structures rather than anything managed.
    /// </summary>
    public static IPAddress? ToIPAddress(this NSData data)
    {
        var bytes = data.ToArray();
        if (bytes.Length < 2)
            return null;

        return bytes[1] switch
        {
            // sockaddr_in: len(1) family(1) port(2) addr(4)
            AfInet when bytes.Length >= 8 => new IPAddress(bytes.AsSpan(4, 4)),

            // sockaddr_in6: len(1) family(1) port(2) flowinfo(4) addr(16)
            AfInet6 when bytes.Length >= 24 => new IPAddress(bytes.AsSpan(8, 16)),

            _ => null
        };
    }


    /// <summary>
    /// Bonjour wants the type and domain with a trailing dot, eg "_http._tcp." and "local.".
    /// </summary>
    public static string ToBonjour(this string value)
        => value.EndsWith('.') ? value : value + ".";


    public static IReadOnlyDictionary<string, string> ToTxtRecords(this NSData? data)
    {
        if (data == null || data.Length == 0)
            return MdnsConstants.EmptyTxt;

        NSDictionary raw;
        try
        {
            raw = NSNetService.DictionaryFromTxtRecord(data);
        }
        catch (Exception)
        {
            // a peer with a malformed TXT record should not sink the whole browse
            return MdnsConstants.EmptyTxt;
        }

        if (raw == null || raw.Count == 0)
            return MdnsConstants.EmptyTxt;

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in raw)
        {
            var key = pair.Key?.ToString();
            if (!String.IsNullOrEmpty(key))
            {
                // values arrive as NSData because TXT values are arbitrary bytes
                var value = pair.Value is NSData bytes
                    ? NSString.FromData(bytes, NSStringEncoding.UTF8)?.ToString() ?? String.Empty
                    : pair.Value?.ToString() ?? String.Empty;

                result.TryAdd(key, value);
            }
        }
        return result;
    }


    public static NSData ToTxtRecordData(this IReadOnlyDictionary<string, string>? values)
    {
        if (values == null || values.Count == 0)
            return new NSData();

        var keys = new List<NSObject>(values.Count);
        var objects = new List<NSObject>(values.Count);

        foreach (var pair in values)
        {
            keys.Add(new NSString(pair.Key));
            objects.Add(NSData.FromString(pair.Value ?? String.Empty, NSStringEncoding.UTF8));
        }

        var dictionary = NSDictionary.FromObjectsAndKeys(objects.ToArray(), keys.ToArray());
        return NSNetService.DataFromTxtRecord(dictionary);
    }
}
