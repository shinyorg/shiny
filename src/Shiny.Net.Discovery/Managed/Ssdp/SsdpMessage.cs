using System.Text;

namespace Shiny.Net.Discovery.Managed.Ssdp;


enum SsdpMessageKind
{
    /// <summary>An M-SEARCH request.</summary>
    Search,

    /// <summary>A NOTIFY advertisement.</summary>
    Notify,

    /// <summary>A 200 OK reply to an M-SEARCH.</summary>
    Response
}


/// <summary>
/// One SSDP datagram. The wire format is HTTP-shaped but is not HTTP - it is a request or status
/// line, headers, and nothing else (HTTPU, UPnP Device Architecture section 1).
/// </summary>
/// <remarks>
/// Parsing here is deliberately forgiving. A decade of shipped consumer hardware sends bare LF line
/// endings, unquoted MAN values, duplicate headers, trailing NUL padding and mixed header casing;
/// rejecting any of that just means not finding devices that every other client on the network can
/// see. Only LOCATION, NT/ST and USN are ever treated as required, and only by the callers.
/// </remarks>
sealed class SsdpMessage
{
    /// <summary>Nothing legitimate is larger, and this bounds what a hostile sender can make us hold.</summary>
    public const int MaxMessageSize = 8 * 1024;

    const int MaxHeaders = 64;
    const int MaxLineLength = 4096;

    // never throws on bad bytes - a mangled SERVER header should not lose us the whole datagram
    static readonly UTF8Encoding encoding = new(false, false);

    SsdpMessage(SsdpMessageKind kind, string startLine, Dictionary<string, string> headers)
    {
        this.Kind = kind;
        this.StartLine = startLine;
        this.Headers = headers;
    }


    public SsdpMessageKind Kind { get; }

    public string StartLine { get; }

    /// <summary>Header names are case-insensitive; the first value of a repeated header wins.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }


    public string? this[string name]
        => this.Headers.TryGetValue(name, out var value) ? value : null;


    /// <summary>
    /// Parses a received datagram, returning false when it is not SSDP at all. Never throws.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<byte> data, out SsdpMessage? message)
    {
        message = null;

        // some stacks send a NUL-terminated buffer
        while (data.Length > 0 && data[^1] == 0)
            data = data[..^1];

        if (data.Length == 0 || data.Length > MaxMessageSize)
            return false;

        var text = encoding.GetString(data);
        var lines = text.Split('\n');

        var startLine = lines[0].TrimEnd('\r').Trim();
        if (startLine.Length == 0 || startLine.Length > MaxLineLength)
            return false;

        if (!TryClassify(startLine, out var kind))
            return false;

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < lines.Length && headers.Count < MaxHeaders; i++)
        {
            var line = lines[i].TrimEnd('\r');

            // the blank line ends the message - anything past it is another datagram or padding
            if (line.Trim().Length == 0)
                break;

            if (line.Length <= MaxLineLength)
                AddHeader(headers, line);
        }

        message = new SsdpMessage(kind, startLine, headers);
        return true;
    }


    static void AddHeader(Dictionary<string, string> headers, string line)
    {
        var separator = line.IndexOf(':');
        if (separator <= 0)
            return;

        var name = line[..separator].Trim();
        // EXT: legitimately has an empty value, so an empty right hand side is not an error
        var value = line[(separator + 1)..].Trim();

        if (name.Length > 0 && !headers.ContainsKey(name))
            headers[name] = value;
    }


    static bool TryClassify(string startLine, out SsdpMessageKind kind)
    {
        if (startLine.StartsWith(SsdpConstants.SearchMethod, StringComparison.OrdinalIgnoreCase))
        {
            kind = SsdpMessageKind.Search;
            return true;
        }

        if (startLine.StartsWith(SsdpConstants.NotifyMethod, StringComparison.OrdinalIgnoreCase))
        {
            kind = SsdpMessageKind.Notify;
            return true;
        }

        if (startLine.StartsWith("HTTP/1.", StringComparison.OrdinalIgnoreCase))
        {
            kind = SsdpMessageKind.Response;
            return true;
        }

        kind = default;
        return false;
    }


    /// <summary>
    /// Reads CACHE-CONTROL's max-age. Tolerates spacing, casing and extra directives, and clamps
    /// the result - devices really do advertise a max-age of seven days or of one second.
    /// </summary>
    public TimeSpan GetMaxAge(TimeSpan fallback)
    {
        var raw = this["CACHE-CONTROL"];
        if (String.IsNullOrWhiteSpace(raw))
            return fallback;

        foreach (var directive in raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = directive.IndexOf('=');
            if (separator > 0 &&
                directive[..separator].Trim().Equals("max-age", StringComparison.OrdinalIgnoreCase) &&
                Int32.TryParse(directive[(separator + 1)..].Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
            {
                return TimeSpan.FromSeconds(Math.Clamp(seconds, 60, 24 * 60 * 60));
            }
        }
        return fallback;
    }


    /// <summary>
    /// Reads one of the 31-bit unsigned UPnP 1.1 headers (BOOTID, CONFIGID, NEXTBOOTID).
    /// </summary>
    public uint? GetUInt(string name)
    {
        var raw = this[name];
        if (String.IsNullOrWhiteSpace(raw))
            return null;

        return UInt32.TryParse(raw.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) && value <= Int32.MaxValue
            ? value
            : null;
    }


    public override string ToString() => $"{this.Kind}: {this.StartLine} ({this.Headers.Count} headers)";
}
