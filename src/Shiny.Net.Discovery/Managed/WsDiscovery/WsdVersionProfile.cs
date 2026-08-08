namespace Shiny.Net.Discovery.Managed.WsDiscovery;


/// <summary>
/// Everything that differs between the 2005 and 2009 flavours of WS-Discovery.
/// </summary>
/// <remarks>
/// It is not just the discovery namespace - the WS-Addressing namespace, the multicast To URI and
/// the anonymous reply URI all changed too, so a message is entirely one version or entirely the
/// other. Probes are therefore sent as two datagrams rather than one hybrid envelope, and replies
/// are answered in whichever version they arrived in.
/// </remarks>
sealed record WsdVersionProfile(
    WsDiscoveryProfile Profile,
    string DiscoveryNamespace,
    string AddressingNamespace,
    string ToUri,
    string AnonymousUri,
    string DefaultMatchBy
)
{
    public const string SoapNamespace = "http://www.w3.org/2003/05/soap-envelope";

    public static readonly WsdVersionProfile Ws2005 = new(
        WsDiscoveryProfile.Ws2005,
        "http://schemas.xmlsoap.org/ws/2005/04/discovery",
        "http://schemas.xmlsoap.org/ws/2004/08/addressing",
        "urn:schemas-xmlsoap-org:ws:2005:04:discovery",
        "http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous",
        "http://schemas.xmlsoap.org/ws/2005/04/discovery/rfc2396"
    );

    public static readonly WsdVersionProfile Ws2009 = new(
        WsDiscoveryProfile.Ws2009,
        "http://docs.oasis-open.org/ws-dd/ns/discovery/2009/01",
        "http://www.w3.org/2005/08/addressing",
        "urn:docs-oasis-open-org:ws-dd:ns:discovery:2009:01",
        "http://www.w3.org/2005/08/addressing/anonymous",
        "http://docs.oasis-open.org/ws-dd/ns/discovery/2009/01/rfc3986"
    );

    public static readonly IReadOnlyList<WsdVersionProfile> All = new[] { Ws2005, Ws2009 };


    /// <summary>The action URI for a message name, eg "Probe".</summary>
    public string ActionFor(string messageName) => $"{this.DiscoveryNamespace}/{messageName}";


    /// <summary>
    /// Picks the profile a received envelope belongs to from its discovery namespace.
    /// </summary>
    public static WsdVersionProfile? ForNamespace(string? discoveryNamespace)
        => All.FirstOrDefault(x => x.DiscoveryNamespace.Equals(discoveryNamespace, StringComparison.OrdinalIgnoreCase));


    /// <summary>
    /// Expands a profile flag set into the profiles to actually transmit.
    /// </summary>
    public static IEnumerable<WsdVersionProfile> Selected(WsDiscoveryProfile profiles)
        => All.Where(x => (profiles & x.Profile) != 0);
}


/// <summary>
/// The WS-Discovery message names, which are also the trailing segment of every action URI.
/// </summary>
static class WsdMessageNames
{
    public const string Probe = "Probe";
    public const string ProbeMatches = "ProbeMatches";
    public const string Resolve = "Resolve";
    public const string ResolveMatches = "ResolveMatches";
    public const string Hello = "Hello";
    public const string Bye = "Bye";
}
