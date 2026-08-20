using System.Net;

namespace Shiny.Net.Wifi;


/// <summary>
/// How to raise a hotspot.
/// </summary>
/// <remarks>
/// Only honoured where <see cref="WifiCapabilities.HotspotCustomConfiguration"/> is set. Android's
/// local-only hotspot generates its own SSID and passphrase and gives you no say; pass null (or
/// nothing at all) there and read the real values back off <see cref="HotspotInfo"/>.
/// </remarks>
public sealed record HotspotConfiguration
{
    /// <summary>The network name to advertise. Null lets the platform choose.</summary>
    public string? Ssid { get; init; }

    /// <summary>
    /// The pre-shared key clients must supply, 8-63 characters. Null lets the platform choose,
    /// which on most platforms means generating one rather than opening the network.
    /// </summary>
    public string? Passphrase { get; init; }

    /// <summary>The band to run on. <see cref="WifiBand.Unknown"/> lets the platform choose.</summary>
    public WifiBand Band { get; init; } = WifiBand.Unknown;

    /// <summary>Suppress SSID broadcast. Ignored on Windows.</summary>
    public bool IsHidden { get; init; }
}


/// <summary>
/// The hotspot as it is actually running - which is not necessarily what you asked for.
/// </summary>
public sealed record HotspotInfo
{
    /// <summary>The advertised network name.</summary>
    public required string Ssid { get; init; }

    /// <summary>The pre-shared key clients need, or null when the hotspot is open.</summary>
    public string? Passphrase { get; init; }

    /// <summary>The authentication scheme clients must use.</summary>
    public WifiSecurity Security { get; init; } = WifiSecurity.Unknown;

    /// <summary>The band in use, where the platform reports it.</summary>
    public WifiBand Band { get; init; } = WifiBand.Unknown;

    /// <summary>This device's address on the hotspot network, where the platform reports it.</summary>
    public IPAddress? Address { get; init; }
}


/// <summary>
/// A device joined to the hotspot.
/// </summary>
/// <remarks>
/// Which fields are populated depends on how the platform learns about the client. Windows reports
/// the MAC address and whatever host names it has resolved; the Linux backend reads the neighbour
/// table, so it always has a MAC and an address but rarely a name. Neither reports signal strength
/// - no OS exposes per-client RSSI to an unprivileged app.
/// </remarks>
public sealed record HotspotClient
{
    /// <summary>The client's MAC address.</summary>
    public required string MacAddress { get; init; }

    /// <summary>The address the client was given on the hotspot network, where known.</summary>
    public IPAddress? IpAddress { get; init; }

    /// <summary>The client's host name, where the platform resolved one.</summary>
    public string? HostName { get; init; }
}


/// <summary>
/// A running hotspot. Dispose it - or call <see cref="Stop"/> - to bring the access point down.
/// </summary>
/// <remarks>
/// The session is the hotspot's lifetime, not a handle to it. Android in particular tears the
/// local-only hotspot down the moment the owning process drops its reservation, so letting this go
/// out of scope without disposing leaves the hotspot up only until your process exits.
/// </remarks>
public interface IHotspotSession : IAsyncDisposable
{
    /// <summary>The hotspot's actual settings, including any the platform chose for you.</summary>
    HotspotInfo Info { get; }

    /// <summary>False once the hotspot has been stopped, by you or by the OS.</summary>
    bool IsRunning { get; }

    /// <summary>
    /// The devices currently joined to the hotspot.
    /// </summary>
    /// <remarks>
    /// A snapshot, not a subscription - poll it if you want a live count. Clients that have
    /// associated but not yet taken a DHCP lease may not appear until they do.
    /// </remarks>
    /// <exception cref="WifiNotSupportedException">
    /// Android, which offers no client list and blocks the ARP table apps used to read instead.
    /// Check <see cref="WifiCapabilities.HotspotClients"/> first.
    /// </exception>
    Task<IReadOnlyList<HotspotClient>> GetClients(CancellationToken ct = default);

    /// <summary>Brings the access point down. Safe to call more than once.</summary>
    Task Stop(CancellationToken ct = default);
}
