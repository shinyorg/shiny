namespace Shiny.Net.Wifi;


/// <summary>
/// A network the device has saved and can rejoin without being handed the passphrase again.
/// </summary>
/// <remarks>
/// <para>The scope of "known" differs by platform and is worth knowing before you build a manage
/// screen on top of it. iOS and Mac Catalyst only ever report the networks <b>your own app</b>
/// configured through NEHotspotConfiguration - the user's own saved networks are invisible. Android
/// likewise reports your app's suggestions and configurations, never the system list. Windows,
/// macOS and Linux report every profile on the machine, whoever created it. Use
/// <see cref="AddedByThisApp"/> to tell the two apart rather than assuming either.</para>
/// <para>Listing needs <see cref="WifiCapabilities.KnownNetworks"/>.</para>
/// </remarks>
public sealed record KnownWifiNetwork
{
    /// <summary>
    /// The handle to pass back to <see cref="IWifiManager.Forget"/> and
    /// <see cref="IWifiManager.Connect(string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Opaque and issued by the platform - a NetworkManager connection UUID on Linux, a numeric
    /// network id on Android below API 29, and the SSID everywhere else. Treat it as a token to
    /// round-trip, not a value to parse or construct: it is stable for as long as the profile
    /// exists but carries no meaning across platforms, and is not necessarily stable across a
    /// forget-and-re-add. Match on <see cref="Ssid"/> if you need to find a network by name.
    /// </remarks>
    public required string Id { get; init; }

    /// <summary>The network name.</summary>
    public required string Ssid { get; init; }

    /// <summary>
    /// The authentication scheme the profile was saved with, or <see cref="WifiSecurity.Unknown"/>
    /// where the platform does not report it - which iOS never does.
    /// </summary>
    public WifiSecurity Security { get; init; } = WifiSecurity.Unknown;

    /// <summary>True when the profile is marked as a hidden network.</summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// True when this app created the profile, and false when it was already on the device.
    /// </summary>
    /// <remarks>
    /// Always true on iOS, Mac Catalyst and Android, which only disclose your own app's entries.
    /// On Windows, macOS and Linux the whole machine's profiles come back and there is no way to
    /// attribute them, so this is false throughout - including for profiles you added yourself.
    /// </remarks>
    public bool AddedByThisApp { get; init; }
}
