namespace Shiny.Net.Discovery;


/// <summary>
/// Discovers and advertises UPnP devices on the local network over SSDP.
/// </summary>
/// <remarks>
/// <para>Unlike <see cref="IMdnsManager"/> this is a managed implementation on every platform,
/// because no operating system exposes an SSDP API - Android's NsdManager and Apple's Bonjour
/// stack are strictly mDNS/DNS-SD.</para>
/// <para>That means raw multicast, and raw multicast has requirements: the
/// <c>com.apple.developer.networking.multicast</c> entitlement on iOS (which Apple grants on
/// request, per developer team), <c>CHANGE_WIFI_MULTICAST_STATE</c> plus - from Android 17 -
/// the <c>ACCESS_LOCAL_NETWORK</c> runtime permission on Android, and the usual firewall and
/// sandbox entitlements elsewhere. A missing one surfaces as
/// <see cref="DiscoveryPermissionException"/> rather than as an empty network.</para>
/// </remarks>
public interface ISsdpManager
{
    /// <summary>
    /// Continuously discovers devices. The enumerable never completes on its own - it runs until
    /// the token is cancelled or you stop enumerating.
    /// </summary>
    /// <param name="config">What to search for and how.</param>
    /// <param name="ct">Cancel to stop browsing.</param>
    /// <returns>A stream of appear/disappear changes.</returns>
    /// <example>
    /// <code>
    /// await foreach (var result in ssdp.Browse(new SsdpBrowseConfig(SsdpConstants.RootDevice), ct))
    ///     Console.WriteLine($"{result.Status}: {result.Device}");
    /// </code>
    /// </example>
    IAsyncEnumerable<SsdpBrowseResult> Browse(SsdpBrowseConfig config, CancellationToken ct = default);

    /// <summary>
    /// Fetches and parses a device's description document, which is where the friendly name,
    /// model information, icons and service list live.
    /// </summary>
    /// <param name="device">A device from <see cref="Browse"/>.</param>
    /// <param name="urlFilter">
    /// Overrides the policy deciding whether the advertised LOCATION may be fetched. The default
    /// accepts only http/https whose host is the literal IP address the advertisement came from,
    /// which stops a device on the network from pointing this library at an arbitrary host.
    /// Replace it only if your devices legitimately advertise a different host - you are taking
    /// on that check yourself.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed description.</returns>
    /// <exception cref="SsdpException">
    /// The device has no location, the location was refused by the fetch policy, or the document
    /// could not be retrieved or parsed.
    /// </exception>
    Task<UpnpDeviceDescription> GetDescription(
        SsdpDevice device,
        Func<Uri, System.Net.IPAddress, bool>? urlFilter = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Advertises a device on the local network. You are responsible for serving the description
    /// document - this only announces it.
    /// </summary>
    /// <param name="registration">What to advertise.</param>
    /// <param name="ct">Cancellation token covering the initial announcement.</param>
    /// <returns>
    /// A handle that keeps the advertisement alive. Dispose it to send goodbyes and stop.
    /// </returns>
    Task<ISsdpPublication> Publish(SsdpDeviceRegistration registration, CancellationToken ct = default);
}


/// <summary>
/// A live SSDP advertisement. Dispose it to send goodbyes and stop advertising.
/// </summary>
public interface ISsdpPublication : IAsyncDisposable
{
    /// <summary>
    /// The unique device name being advertised.
    /// </summary>
    string Udn { get; }

    /// <summary>
    /// The description URL being advertised.
    /// </summary>
    Uri Location { get; }

    /// <summary>
    /// Every notification type being advertised - the root device marker, the UDN, and any
    /// device and service types.
    /// </summary>
    IReadOnlyList<string> NotificationTypes { get; }

    /// <summary>
    /// The current BOOTID.UPNP.ORG. It increases whenever the host's addresses change, which is
    /// how control points tell a restart from a duplicate announcement.
    /// </summary>
    uint BootId { get; }
}
