namespace Shiny.Nfc;


/// <summary>
/// The NFC Forum URI record prefix table (URI RTD 1.0, section 3.2.2). The index is the code.
/// </summary>
static class NdefUriPrefixes
{
    static readonly string[] Prefixes =
    [
        "",
        "http://www.",
        "https://www.",
        "http://",
        "https://",
        "tel:",
        "mailto:",
        "ftp://anonymous:anonymous@",
        "ftp://ftp.",
        "ftps://",
        "sftp://",
        "smb://",
        "nfs://",
        "ftp://",
        "dav://",
        "news:",
        "telnet://",
        "imap:",
        "rtsp://",
        "urn:",
        "pop:",
        "sip:",
        "sips:",
        "tftp:",
        "btspp://",
        "btl2cap://",
        "btgoep://",
        "tcpobex://",
        "irdaobex://",
        "file://",
        "urn:epc:id:",
        "urn:epc:tag:",
        "urn:epc:pat:",
        "urn:epc:raw:",
        "urn:epc:",
        "urn:nfc:"
    ];


    /// <summary>The prefix for a code, or null for a code the table reserves.</summary>
    public static string? Get(byte code) => code < Prefixes.Length ? Prefixes[code] : null;


    /// <summary>
    /// The longest prefix the URI starts with, matched case-sensitively so the URI reads back
    /// exactly as it was written.
    /// </summary>
    public static (byte Code, string Prefix) Match(string uri)
    {
        var best = 0;
        for (var i = 1; i < Prefixes.Length; i++)
        {
            if (Prefixes[i].Length > Prefixes[best].Length && uri.StartsWith(Prefixes[i], StringComparison.Ordinal))
                best = i;
        }
        return ((byte)best, Prefixes[best]);
    }
}
