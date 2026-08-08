using System.Globalization;
using System.Text;

namespace Shiny.Net.Discovery.Managed.Ssdp;


/// <summary>
/// Formats the four datagrams SSDP needs: the search, the two advertisement flavours, and the
/// unicast reply to a search.
/// </summary>
/// <remarks>
/// Everything is CRLF terminated and ends with a blank line. Header order follows the UPnP Device
/// Architecture examples - it is not significant, but matching the spec's ordering keeps captures
/// readable next to other stacks.
/// </remarks>
static class SsdpMessageBuilder
{
    /// <summary>Identifies us in SERVER and USER-AGENT, per UDA section 1.1.2.</summary>
    static readonly string productTokens = $"{BuildOsToken()} UPnP/1.1 Shiny.Net.Discovery/1.0";


    public static byte[] Search(string searchTarget, int maxWaitSeconds, string host, string? friendlyName)
    {
        var builder = new StringBuilder()
            .Append("M-SEARCH * HTTP/1.1\r\n")
            .Append("HOST: ").Append(host).Append("\r\n")
            // the quotes are required by the spec even though most devices tolerate their absence
            .Append("MAN: ").Append(SsdpConstants.DiscoverMan).Append("\r\n")
            .Append("MX: ").Append(maxWaitSeconds.ToString(CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("ST: ").Append(searchTarget).Append("\r\n")
            .Append("USER-AGENT: ").Append(productTokens).Append("\r\n");

        if (!String.IsNullOrWhiteSpace(friendlyName))
            builder.Append("CPFN.UPNP.ORG: ").Append(friendlyName).Append("\r\n");

        return Finish(builder);
    }


    public static byte[] Alive(
        string host,
        string notificationType,
        string usn,
        Uri location,
        TimeSpan maxAge,
        uint bootId,
        uint configId,
        string? server
    )
    {
        var builder = new StringBuilder()
            .Append("NOTIFY * HTTP/1.1\r\n")
            .Append("HOST: ").Append(host).Append("\r\n")
            .Append("CACHE-CONTROL: max-age=").Append(((int)maxAge.TotalSeconds).ToString(CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("LOCATION: ").Append(location.AbsoluteUri).Append("\r\n")
            .Append("NT: ").Append(notificationType).Append("\r\n")
            .Append("NTS: ").Append(SsdpConstants.AliveNts).Append("\r\n")
            .Append("SERVER: ").Append(server ?? productTokens).Append("\r\n")
            .Append("USN: ").Append(usn).Append("\r\n")
            .Append("BOOTID.UPNP.ORG: ").Append(bootId.ToString(CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("CONFIGID.UPNP.ORG: ").Append(configId.ToString(CultureInfo.InvariantCulture)).Append("\r\n");

        return Finish(builder);
    }


    /// <summary>
    /// A goodbye carries no LOCATION, CACHE-CONTROL or SERVER - receivers must not require them.
    /// </summary>
    public static byte[] ByeBye(string host, string notificationType, string usn, uint bootId, uint configId)
    {
        var builder = new StringBuilder()
            .Append("NOTIFY * HTTP/1.1\r\n")
            .Append("HOST: ").Append(host).Append("\r\n")
            .Append("NT: ").Append(notificationType).Append("\r\n")
            .Append("NTS: ").Append(SsdpConstants.ByeByeNts).Append("\r\n")
            .Append("USN: ").Append(usn).Append("\r\n")
            .Append("BOOTID.UPNP.ORG: ").Append(bootId.ToString(CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("CONFIGID.UPNP.ORG: ").Append(configId.ToString(CultureInfo.InvariantCulture)).Append("\r\n");

        return Finish(builder);
    }


    /// <summary>
    /// The unicast answer to an M-SEARCH. <paramref name="searchTarget"/> echoes what the searcher
    /// asked for rather than what we advertise, which matters for the version downgrade rule.
    /// </summary>
    public static byte[] SearchResponse(
        string searchTarget,
        string usn,
        Uri location,
        TimeSpan maxAge,
        uint bootId,
        uint configId,
        string? server
    )
    {
        var builder = new StringBuilder()
            .Append("HTTP/1.1 200 OK\r\n")
            .Append("CACHE-CONTROL: max-age=").Append(((int)maxAge.TotalSeconds).ToString(CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("DATE: ").Append(DateTimeOffset.UtcNow.ToString("r", CultureInfo.InvariantCulture)).Append("\r\n")
            // EXT is required and its value is deliberately empty
            .Append("EXT:\r\n")
            .Append("LOCATION: ").Append(location.AbsoluteUri).Append("\r\n")
            .Append("SERVER: ").Append(server ?? productTokens).Append("\r\n")
            .Append("ST: ").Append(searchTarget).Append("\r\n")
            .Append("USN: ").Append(usn).Append("\r\n")
            .Append("BOOTID.UPNP.ORG: ").Append(bootId.ToString(CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("CONFIGID.UPNP.ORG: ").Append(configId.ToString(CultureInfo.InvariantCulture)).Append("\r\n");

        return Finish(builder);
    }


    static byte[] Finish(StringBuilder builder)
        => Encoding.UTF8.GetBytes(builder.Append("\r\n").ToString());


    static string BuildOsToken()
    {
        var name = OperatingSystem.IsWindows() ? "Windows"
            : OperatingSystem.IsAndroid() ? "Android"
            : OperatingSystem.IsIOS() ? "iOS"
            : OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS() ? "macOS"
            : OperatingSystem.IsLinux() ? "Linux"
            : "Unix";

        return $"{name}/{Environment.OSVersion.Version.ToString(2)}";
    }
}
