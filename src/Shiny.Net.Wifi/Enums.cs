namespace Shiny.Net.Wifi;


/// <summary>
/// The Wi-Fi operations the current platform can actually perform.
/// </summary>
/// <remarks>
/// <para>Wi-Fi is the most unevenly exposed capability across the platforms Shiny targets. iOS has
/// no scanning API at all outside an Apple-granted NEHotspotHelper entitlement, only Android and
/// Windows can raise a hotspot, and "known networks" means something different on all six.
/// Rather than pretend otherwise, every manager reports what it can do here and throws
/// <see cref="WifiNotSupportedException"/> - naming the specific platform limit - for the rest.</para>
/// <para>Check the flag before offering the feature in your UI; catching the exception afterwards
/// is the fallback, not the plan.</para>
/// </remarks>
[Flags]
public enum WifiCapabilities
{
    /// <summary>Nothing is available. Usually means no Wi-Fi hardware, or a platform with no Wi-Fi API.</summary>
    None = 0,

    /// <summary><see cref="IWifiManager.Scan"/> returns the access points in range.</summary>
    Scan = 1,

    /// <summary>
    /// <see cref="IWifiManager.Connect(WifiConnectionRequest, CancellationToken)"/> can join a
    /// named network.
    /// </summary>
    Connect = 2,

    /// <summary><see cref="IWifiManager.Disconnect"/> can leave the current network.</summary>
    Disconnect = 4,

    /// <summary><see cref="IWifiManager.GetCurrentNetwork"/> reports the joined network's SSID.</summary>
    /// <remarks>
    /// IP and DNS details come from the managed network stack and are available even without this
    /// flag - it is the SSID/BSSID specifically that needs platform permission.
    /// </remarks>
    CurrentNetwork = 8,

    /// <summary><see cref="IWifiManager.GetRadioEnabled"/> reports whether the Wi-Fi radio is on.</summary>
    RadioState = 16,

    /// <summary><see cref="IWifiManager.SetRadioEnabled"/> can power the Wi-Fi radio on and off.</summary>
    RadioToggle = 32,

    /// <summary><see cref="IWifiHotspot.Start"/> can raise an access point.</summary>
    Hotspot = 64,

    /// <summary>
    /// The hotspot honours the SSID and passphrase you supply. Without this flag the OS picks
    /// both and only reports them back to you - Android's local-only hotspot works this way.
    /// </summary>
    HotspotCustomConfiguration = 128,

    /// <summary>
    /// <see cref="IHotspotSession.GetClients"/> can enumerate the devices joined to the hotspot.
    /// Windows and Linux only - Android exposes no client list and blocks the ARP table it would
    /// otherwise be read from.
    /// </summary>
    HotspotClients = 256,

    /// <summary>
    /// <see cref="IWifiManager.GetKnownNetworks"/> can list the networks saved on the device.
    /// </summary>
    /// <remarks>
    /// What "known" covers is platform-specific: iOS and Mac Catalyst see only the networks your
    /// own app configured, Android sees your app's suggestions and configurations, and Windows,
    /// macOS and Linux see every profile on the machine.
    /// </remarks>
    KnownNetworks = 512,

    /// <summary><see cref="IWifiManager.Forget"/> can delete a saved network.</summary>
    ForgetNetwork = 1024,

    /// <summary>
    /// <see cref="IWifiManager.Connect(string, CancellationToken)"/> can rejoin a saved network by
    /// id, without being handed the passphrase again.
    /// </summary>
    /// <remarks>
    /// Absent on iOS and modern Android, where a saved network is a standing hint the OS acts on
    /// when it chooses - there is no call to make it join one on demand.
    /// </remarks>
    ConnectKnownNetwork = 2048
}


/// <summary>
/// The authentication scheme an access point advertises.
/// </summary>
/// <remarks>
/// Platforms report security at wildly different resolutions - Android hands back a capability
/// string, NetworkManager hands back two bitfields, WinRT hands back a single enum - so this is
/// deliberately coarse. Anything that cannot be mapped confidently comes back as
/// <see cref="Unknown"/> rather than being guessed at.
/// </remarks>
public enum WifiSecurity
{
    /// <summary>The scheme could not be determined.</summary>
    Unknown,

    /// <summary>No authentication - an open network.</summary>
    Open,

    /// <summary>WEP. Broken; treat a network advertising this as open.</summary>
    Wep,

    /// <summary>WPA personal (TKIP-era pre-shared key).</summary>
    WpaPsk,

    /// <summary>WPA2 personal (pre-shared key).</summary>
    Wpa2Psk,

    /// <summary>WPA3 personal (SAE).</summary>
    Wpa3Psk,

    /// <summary>802.1X / EAP, any WPA generation.</summary>
    Enterprise,

    /// <summary>Opportunistic Wireless Encryption - unauthenticated but encrypted.</summary>
    Owe,

    /// <summary>
    /// A pre-shared key of an unnamed generation - the network is personal rather than open or
    /// enterprise, but the platform did not say which of WPA, WPA2 or WPA3 is in use.
    /// </summary>
    /// <remarks>
    /// iOS is the one platform that reports at this resolution: <c>NEHotspotNetwork</c> answers
    /// "personal" and nothing finer. Reporting it as <see cref="Unknown"/> would throw away the
    /// part iOS does know, and picking one of the three would be a guess.
    /// </remarks>
    Psk
}


/// <summary>
/// The radio band an access point or hotspot operates on.
/// </summary>
public enum WifiBand
{
    /// <summary>Not reported, or a frequency outside the known Wi-Fi bands.</summary>
    Unknown,

    /// <summary>2.4 GHz - longer range, more congested.</summary>
    TwoPointFourGhz,

    /// <summary>5 GHz - shorter range, more throughput.</summary>
    FiveGhz,

    /// <summary>6 GHz (Wi-Fi 6E and later).</summary>
    SixGhz
}
