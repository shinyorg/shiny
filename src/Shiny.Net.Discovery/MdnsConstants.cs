namespace Shiny.Net.Discovery;


/// <summary>
/// Well known mDNS/DNS-SD values.
/// </summary>
public static class MdnsConstants
{
    /// <summary>
    /// The only domain multicast DNS operates in.
    /// </summary>
    public const string LocalDomain = "local";

    /// <summary>
    /// The meta-query service type used to enumerate every service type being advertised
    /// on the link. Browsing this yields service types rather than service instances.
    /// </summary>
    public const string ServiceTypeEnumerationQuery = "_services._dns-sd._udp";

    /// <summary>
    /// The IPv4 multicast group mDNS operates on.
    /// </summary>
    public const string IPv4MulticastAddress = "224.0.0.251";

    /// <summary>
    /// The IPv6 multicast group mDNS operates on.
    /// </summary>
    public const string IPv6MulticastAddress = "ff02::fb";

    /// <summary>
    /// The port mDNS operates on.
    /// </summary>
    public const int Port = 5353;

    internal static readonly IReadOnlyDictionary<string, string> EmptyTxt =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
