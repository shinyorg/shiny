namespace Shiny.Net.Wifi;


/// <summary>
/// What to join, and how.
/// </summary>
/// <remarks>
/// Platforms differ sharply in how much of this they honour. iOS uses the SSID and passphrase and
/// ignores everything else. Android 10+ shows the user a system dialog naming the network and joins
/// only for as long as your app holds the request. Windows, macOS and Linux honour all of it.
/// </remarks>
public sealed record WifiConnectionRequest
{
    /// <param name="ssid">The network name to join.</param>
    public WifiConnectionRequest(string ssid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ssid);
        this.Ssid = ssid;
    }

    /// <summary>The network name to join.</summary>
    public string Ssid { get; }

    /// <summary>The pre-shared key. Leave null for an open network.</summary>
    public string? Passphrase { get; init; }

    /// <summary>
    /// The scheme to authenticate with. Leave <see cref="WifiSecurity.Unknown"/> to let the
    /// platform work it out from the beacon - which is what you want unless you are joining a
    /// hidden network, where there is no beacon to read.
    /// </summary>
    public WifiSecurity Security { get; init; } = WifiSecurity.Unknown;

    /// <summary>Pin the join to one specific access point. Ignored on iOS.</summary>
    public string? Bssid { get; init; }

    /// <summary>True when the network does not broadcast its SSID.</summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// Save the network so the OS rejoins it automatically later. Ignored on Android 10+, where a
    /// specifier-based join is never persisted, and on iOS, where the configuration lives until
    /// your app is deleted or calls <see cref="IWifiManager.Disconnect"/>.
    /// </summary>
    public bool Remember { get; init; } = true;

    /// <summary>How long to wait for association before giving up. Defaults to 30 seconds.</summary>
    public TimeSpan? Timeout { get; init; }
}
