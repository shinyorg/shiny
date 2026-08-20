namespace Shiny.Net.Wifi;


/// <summary>
/// Raises a Wi-Fi access point other devices can join.
/// </summary>
/// <remarks>
/// <para>Available on Android (local-only hotspot - the clients reach this device but get no
/// internet), Windows (real tethering, sharing the current internet connection) and Linux
/// (NetworkManager AP mode). iOS and macOS expose no hotspot API to apps at all.</para>
/// <para>Check <see cref="WifiCapabilities.Hotspot"/> before offering this, and
/// <see cref="WifiCapabilities.HotspotCustomConfiguration"/> before letting the user name the
/// network - Android picks the SSID and passphrase itself.</para>
/// </remarks>
public interface IWifiHotspot
{
    /// <summary>Whether this platform can raise a hotspot at all.</summary>
    bool IsSupported { get; }

    /// <summary>The running hotspot's settings, or null when none is running.</summary>
    HotspotInfo? Current { get; }

    /// <summary>Fires when a hotspot starts (with its settings) or stops (with null).</summary>
    event EventHandler<HotspotInfo?>? Changed;

    /// <summary>
    /// Raises an access point and waits for it to come up.
    /// </summary>
    /// <param name="config">
    /// The SSID, passphrase and band to use. Ignored where the platform chooses its own - read the
    /// returned <see cref="IHotspotSession.Info"/> for what it actually picked.
    /// </param>
    /// <param name="ct">Cancels the attempt.</param>
    /// <returns>The running hotspot. Dispose it to bring the access point down.</returns>
    /// <exception cref="WifiNotSupportedException">iOS and macOS, which have no hotspot API.</exception>
    Task<IHotspotSession> Start(HotspotConfiguration? config = null, CancellationToken ct = default);

    /// <summary>Stops the running hotspot, if there is one. A no-op otherwise.</summary>
    Task Stop(CancellationToken ct = default);
}
