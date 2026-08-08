using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Shiny.Net.Discovery;


/// <summary>
/// Why a browse result was emitted.
/// </summary>
public enum SsdpBrowseStatus
{
    /// <summary>
    /// The device appeared, or re-announced itself with something changed. Treat this as an
    /// upsert keyed on <see cref="SsdpDevice.Udn"/>.
    /// </summary>
    Found,

    /// <summary>
    /// The device sent a goodbye or its advertisement expired. Only
    /// <see cref="SsdpDevice.Udn"/> is guaranteed to be populated.
    /// </summary>
    Lost
}


/// <summary>
/// A single change emitted by <see cref="ISsdpManager.Browse(SsdpBrowseConfig, CancellationToken)"/>.
/// </summary>
/// <param name="Status">Whether the device appeared or went away.</param>
/// <param name="Device">The device the change applies to.</param>
public sealed record SsdpBrowseResult(SsdpBrowseStatus Status, SsdpDevice Device);


/// <summary>
/// Options for an SSDP browse.
/// </summary>
public sealed record SsdpBrowseConfig
{
    public SsdpBrowseConfig() { }

    /// <param name="searchTarget">
    /// What to search for - <see cref="SsdpConstants.SearchAll"/>,
    /// <see cref="SsdpConstants.RootDevice"/>, a "uuid:..." UDN, or a device/service type URN.
    /// </param>
    [SetsRequiredMembers]
    public SsdpBrowseConfig(string searchTarget)
        => this.SearchTarget = searchTarget;

    /// <summary>
    /// The search target. Defaults to <see cref="SsdpConstants.SearchAll"/>, which finds
    /// everything at the cost of several datagrams per device.
    /// </summary>
    public string SearchTarget { get; init; } = SsdpConstants.SearchAll;

    /// <summary>
    /// The MX header - how long devices may wait before answering, so they do not all reply at
    /// once. The spec caps this at 5 seconds; below 2 you will miss slow embedded devices.
    /// </summary>
    public TimeSpan MaxWait { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How often the search is repeated while browsing. Devices that were asleep or on another
    /// network when the last search went out are picked up by the next one.
    /// </summary>
    public TimeSpan SearchInterval { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// When true (the default) the browse also listens for unsolicited NOTIFY advertisements,
    /// which is how devices announce themselves between searches and how goodbyes arrive.
    /// </summary>
    public bool ListenForNotifications { get; init; } = true;

    /// <summary>
    /// A friendly name for this control point, sent as CPFN.UPNP.ORG. Some devices log it.
    /// </summary>
    public string? FriendlyName { get; init; }
}
