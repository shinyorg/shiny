namespace Shiny.Net.Wifi;


/// <summary>
/// Scans for, joins and leaves Wi-Fi networks, and reports the one currently joined.
/// </summary>
/// <remarks>
/// <para>Backed by WifiManager + ConnectivityManager on Android, NEHotspotConfiguration on
/// iOS/Mac Catalyst, CoreWLAN on macOS, the WiFiAdapter WinRT API on Windows and NetworkManager
/// over D-Bus on Linux (via the separate Shiny.Net.Wifi.Linux package).</para>
/// <para>Not every platform can do every operation. Read <see cref="Capabilities"/> before
/// offering a feature; anything unavailable throws <see cref="WifiNotSupportedException"/>.</para>
/// </remarks>
public interface IWifiManager
{
    /// <summary>What this platform can actually do. Check before calling.</summary>
    WifiCapabilities Capabilities { get; }

    /// <summary>
    /// The network currently joined, or null when Wi-Fi is off or unassociated.
    /// </summary>
    /// <remarks>
    /// Read live off the OS on every access, so it is always current but is not free - hold the
    /// result rather than re-reading it in a loop. Addressing details are always populated; SSID
    /// and BSSID need <see cref="WifiCapabilities.CurrentNetwork"/>.
    /// </remarks>
    WifiNetworkInfo? CurrentNetwork { get; }

    /// <summary>
    /// Fires when the joined network changes - a different SSID, a new IP lease, a DNS change, or
    /// dropping off Wi-Fi entirely (which delivers null).
    /// </summary>
    /// <remarks>
    /// Only raised on a real change; the platform watchers behind this are chatty and their
    /// duplicates are filtered out. Listening costs a native watcher, so unsubscribe when you are
    /// done - the last unsubscribe tears it down.
    /// </remarks>
    event EventHandler<WifiNetworkInfo?>? Changed;

    /// <summary>
    /// Asks for whatever the platform needs before the other calls will work - location permission
    /// on Android and macOS, the WiFiAdapter consent prompt on Windows.
    /// </summary>
    /// <remarks>
    /// Safe to call repeatedly; it returns the current state without re-prompting once answered.
    /// Call it before <see cref="Scan"/> in particular, which otherwise returns an empty list
    /// rather than failing.
    /// </remarks>
    Task<AccessState> RequestAccess(CancellationToken ct = default);

    /// <summary>
    /// Scans for access points in range.
    /// </summary>
    /// <param name="ct">Cancels the scan. Results already collected are discarded.</param>
    /// <returns>One entry per BSSID heard, strongest first.</returns>
    /// <exception cref="WifiNotSupportedException">iOS and Mac Catalyst, which have no scanning API.</exception>
    /// <exception cref="WifiPermissionException">The scan needs a permission that has not been granted.</exception>
    Task<IReadOnlyList<WifiNetwork>> Scan(CancellationToken ct = default);

    /// <summary>
    /// Joins a network and waits for it to come up.
    /// </summary>
    /// <param name="request">What to join.</param>
    /// <param name="ct">Cancels the attempt.</param>
    /// <returns>The joined network, with addressing populated.</returns>
    /// <exception cref="WifiConnectionException">The join failed or timed out.</exception>
    Task<WifiNetworkInfo> Connect(WifiConnectionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Leaves the current network.
    /// </summary>
    /// <remarks>
    /// What this means varies: on Android 10+ and iOS it drops the network your app asked for and
    /// the OS is free to rejoin one it already knew about, so the device may not end up offline.
    /// On Windows, macOS and Linux it disassociates the adapter outright.
    /// </remarks>
    Task Disconnect(CancellationToken ct = default);

    /// <summary>Whether the Wi-Fi radio is powered on.</summary>
    /// <exception cref="WifiNotSupportedException">The platform will not disclose radio state.</exception>
    Task<bool> GetRadioEnabled(CancellationToken ct = default);

    /// <summary>
    /// Powers the Wi-Fi radio on or off.
    /// </summary>
    /// <remarks>
    /// Only Windows, macOS and Linux allow this. Android revoked it for third-party apps in API 29
    /// (send the user to the Wi-Fi settings panel instead) and iOS never allowed it.
    /// </remarks>
    /// <exception cref="WifiNotSupportedException">The platform does not let apps toggle the radio.</exception>
    Task SetRadioEnabled(bool enabled, CancellationToken ct = default);
}
