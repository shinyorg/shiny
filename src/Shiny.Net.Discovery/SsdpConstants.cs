namespace Shiny.Net.Discovery;


/// <summary>
/// Well known SSDP/UPnP values.
/// </summary>
public static class SsdpConstants
{
    /// <summary>
    /// The IPv4 multicast group SSDP operates on. Shared with WS-Discovery, which uses a
    /// different port on the same group.
    /// </summary>
    public const string IPv4MulticastAddress = "239.255.255.250";

    /// <summary>
    /// The link-local IPv6 multicast group. UPnP also defines site, organisation and global
    /// scoped groups; only link-local is used here because that is what devices answer on.
    /// </summary>
    public const string IPv6MulticastAddress = "ff02::c";

    /// <summary>
    /// The port SSDP operates on.
    /// </summary>
    public const int Port = 1900;

    /// <summary>
    /// The search target that matches every advertisement on the network. A device answers this
    /// once per advertisement, so expect several datagrams per device.
    /// </summary>
    public const string SearchAll = "ssdp:all";

    /// <summary>
    /// The notification type carried by every UPnP root device. Search for this to enumerate
    /// devices without also enumerating their embedded devices and services.
    /// </summary>
    public const string RootDevice = "upnp:rootdevice";

    /// <summary>
    /// The UPnP Device Architecture default hop limit. Unlike mDNS's 255, SSDP is meant to stay
    /// within a couple of hops.
    /// </summary>
    public const int DefaultTtl = 2;

    /// <summary>
    /// The name of the <see cref="System.Net.Http.HttpClient"/> used to fetch device descriptions.
    /// Call <c>services.AddHttpClient(SsdpConstants.HttpClientName)</c> to configure it.
    /// </summary>
    public const string HttpClientName = "Shiny.Net.Discovery.Ssdp";

    /// <summary>
    /// What a device's advertisement is assumed to be worth when it sends no CACHE-CONTROL,
    /// and the value used when publishing.
    /// </summary>
    public static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(30);

    internal static readonly IReadOnlyDictionary<string, string> EmptyHeaders =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    internal const string SearchMethod = "M-SEARCH";
    internal const string NotifyMethod = "NOTIFY";
    internal const string AliveNts = "ssdp:alive";
    internal const string ByeByeNts = "ssdp:byebye";
    internal const string UpdateNts = "ssdp:update";
    internal const string DiscoverMan = "\"ssdp:discover\"";
}
