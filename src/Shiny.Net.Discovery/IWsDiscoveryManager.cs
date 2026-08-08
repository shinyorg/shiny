using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Shiny.Net.Discovery;


/// <summary>
/// Options for a WS-Discovery browse.
/// </summary>
public sealed record WsdProbeConfig
{
    public WsdProbeConfig() { }

    /// <param name="types">The types to probe for. Empty matches every device.</param>
    [SetsRequiredMembers]
    public WsdProbeConfig(params XmlQualifiedName[] types)
        => this.Types = types;

    /// <summary>
    /// The types a device must implement to match. Empty (the default) matches everything, which
    /// is usually what you want - many devices answer every probe regardless, and filtering
    /// locally finds more hardware than a typed probe does.
    /// </summary>
    public IReadOnlyList<XmlQualifiedName> Types { get; init; } = Array.Empty<XmlQualifiedName>();

    /// <summary>
    /// The scopes a device must be in to match. Empty matches everything.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The scope matching rule URI. Null uses the version's default (RFC 3986 style segment
    /// prefix matching), which is what ONVIF relies on. The LDAP rule is not implemented.
    /// </summary>
    public string? ScopeMatchBy { get; init; }

    /// <summary>
    /// Which specification versions to probe with. Both by default, sent as separate datagrams -
    /// many ONVIF cameras answer only the 2005 one.
    /// </summary>
    public WsDiscoveryProfile Profiles { get; init; } = WsDiscoveryProfile.All;

    /// <summary>
    /// How often the probe is repeated while browsing.
    /// </summary>
    public TimeSpan ProbeInterval { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// When true (the default) the browse also listens for unsolicited Hello and Bye
    /// announcements, which is how devices appear and disappear between probes.
    /// </summary>
    public bool ListenForAnnouncements { get; init; } = true;

    /// <summary>
    /// When true (the default) a target that answers without any transport address is sent a
    /// Resolve to ask for one. Plenty of devices ignore Resolve entirely.
    /// </summary>
    public bool ResolveMissingAddresses { get; init; } = true;
}


/// <summary>
/// Discovers and advertises WS-Discovery target services on the local network - ONVIF cameras,
/// WSD printers and scanners, Windows machines.
/// </summary>
/// <remarks>
/// <para>Like <see cref="ISsdpManager"/> and unlike <see cref="IMdnsManager"/>, this is a managed
/// implementation on every platform, because no operating system exposes a WS-Discovery API to an
/// app. That means raw multicast and the platform requirements that come with it - notably the
/// Apple-granted <c>com.apple.developer.networking.multicast</c> entitlement on iOS. A missing
/// permission surfaces as <see cref="DiscoveryPermissionException"/>.</para>
/// <para>Discovery only. Invoking the device's SOAP services - ONVIF <c>GetCapabilities</c>,
/// <c>GetProfiles</c>, stream URIs - is out of scope; <see cref="WsdTarget.PreferredAddress"/>
/// gives you what a SOAP client needs to take over.</para>
/// </remarks>
public interface IWsDiscoveryManager
{
    /// <summary>
    /// Continuously discovers targets. The enumerable never completes on its own - it runs until
    /// the token is cancelled or you stop enumerating.
    /// </summary>
    /// <param name="config">What to probe for and how.</param>
    /// <param name="ct">Cancel to stop browsing.</param>
    /// <returns>A stream of appear/disappear changes.</returns>
    IAsyncEnumerable<WsDiscoveryResult> Browse(WsdProbeConfig config, CancellationToken ct = default);

    /// <summary>
    /// Asks a specific target to report its current addresses and metadata.
    /// </summary>
    /// <param name="endpointReference">The target's endpoint reference, eg "urn:uuid:...".</param>
    /// <param name="timeout">How long to wait. Defaults to 5 seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The target, or null when it did not answer. Many devices never answer Resolve.</returns>
    Task<WsdTarget?> Resolve(string endpointReference, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Advertises this host as a WS-Discovery target service: Hello on start, answers to matching
    /// Probe and Resolve messages, and Bye on dispose.
    /// </summary>
    /// <param name="registration">What to advertise.</param>
    /// <param name="ct">Cancellation token covering the initial Hello.</param>
    /// <returns>A handle that keeps the advertisement alive.</returns>
    Task<IWsDiscoveryPublication> Publish(WsDiscoveryRegistration registration, CancellationToken ct = default);
}


/// <summary>
/// A live WS-Discovery advertisement. Dispose it to send Bye and stop answering probes.
/// </summary>
public interface IWsDiscoveryPublication : IAsyncDisposable
{
    /// <summary>
    /// The endpoint reference being advertised.
    /// </summary>
    string EndpointReference { get; }

    /// <summary>
    /// The current metadata version. It increases whenever the advertised addresses change.
    /// </summary>
    uint MetadataVersion { get; }
}
