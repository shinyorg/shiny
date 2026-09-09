using System;
using System.Collections.Generic;
using System.Text;

namespace Shiny.Beacons;


/// <summary>
/// Compresses and expands URLs the way the Eddystone-URL specification requires.
/// </summary>
/// <remarks>
/// A BLE advertisement has no room for a URL spelled out in full, so Eddystone substitutes a single
/// byte for the scheme and another for any of thirteen common top level domain endings. What is
/// left has to fit in 17 bytes.
/// </remarks>
public static class EddystoneUrlCodec
{
    /// <summary>The maximum number of bytes the encoded URL body may occupy.</summary>
    public const int MaxEncodedLength = 17;

    static readonly string[] Schemes =
    [
        "http://www.",
        "https://www.",
        "http://",
        "https://"
    ];

    static readonly string[] Expansions =
    [
        ".com/", ".org/", ".edu/", ".net/", ".info/", ".biz/", ".gov/",
        ".com",  ".org",  ".edu",  ".net",  ".info",  ".biz",  ".gov"
    ];


    /// <summary>
    /// Compresses a URL into its Eddystone wire form.
    /// </summary>
    /// <param name="url">The URL. Must start with http:// or https://.</param>
    /// <returns>The scheme byte followed by the encoded body.</returns>
    /// <exception cref="ArgumentException">
    /// The scheme is not one Eddystone can encode, or the compressed result does not fit.
    /// </exception>
    public static byte[] Encode(string url)
    {
        if (url.IsEmpty())
            throw new ArgumentException("A URL is required", nameof(url));

        var schemeIndex = -1;
        for (var i = 0; i < Schemes.Length; i++)
        {
            // longest-prefix wins: "http://www." must beat "http://"
            if (url.StartsWith(Schemes[i], StringComparison.OrdinalIgnoreCase) &&
                (schemeIndex < 0 || Schemes[i].Length > Schemes[schemeIndex].Length))
            {
                schemeIndex = i;
            }
        }

        if (schemeIndex < 0)
            throw new ArgumentException($"'{url}' does not start with a scheme Eddystone can encode (http:// or https://)", nameof(url));

        var body = url.Substring(Schemes[schemeIndex].Length);
        var bytes = new List<byte> { (byte)schemeIndex };
        var position = 0;

        while (position < body.Length)
        {
            var expansion = -1;
            for (var i = 0; i < Expansions.Length; i++)
            {
                if (body.AsSpan(position).StartsWith(Expansions[i], StringComparison.Ordinal) &&
                    (expansion < 0 || Expansions[i].Length > Expansions[expansion].Length))
                {
                    expansion = i;
                }
            }

            if (expansion >= 0)
            {
                bytes.Add((byte)expansion);
                position += Expansions[expansion].Length;
            }
            else
            {
                var ch = body[position];
                if (ch is < (char)0x20 or > (char)0x7E)
                    throw new ArgumentException($"'{url}' contains a character Eddystone cannot encode at position {position}", nameof(url));

                bytes.Add((byte)ch);
                position++;
            }
        }

        // the scheme byte is not counted against the body budget
        if (bytes.Count - 1 > MaxEncodedLength)
            throw new ArgumentException($"'{url}' compresses to {bytes.Count - 1} bytes - Eddystone allows {MaxEncodedLength}", nameof(url));

        return bytes.ToArray();
    }


    /// <summary>
    /// Expands an Eddystone-encoded URL back to its full form.
    /// </summary>
    /// <param name="data">The scheme byte followed by the encoded body.</param>
    /// <returns>The URL, or null when the scheme byte is not one of the four defined values.</returns>
    public static string? Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0 || data[0] >= Schemes.Length)
            return null;

        var builder = new StringBuilder(Schemes[data[0]]);

        for (var i = 1; i < data.Length; i++)
        {
            var b = data[i];

            if (b < Expansions.Length)
                builder.Append(Expansions[b]);

            // 0x0E-0x20 are reserved by the specification and 0x7F+ is not a graphic character;
            // skip rather than throw, so one odd byte does not discard an otherwise readable URL
            else if (b is >= 0x20 and <= 0x7E)
                builder.Append((char)b);
        }

        return builder.ToString();
    }
}
