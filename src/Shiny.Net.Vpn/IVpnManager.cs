namespace Shiny.Net.Vpn;


/// <summary>
/// Installs and drives this app's profile in the operating system's built-in IKEv2 VPN client.
/// </summary>
/// <remarks>
/// <para>The OS owns the tunnel: it keeps running when the app is suspended or closed, appears in the
/// system VPN settings (where the user can switch it off or delete it), and is subject to the OS's own
/// consent prompt the first time a profile is saved. This is not a VPN protocol implementation - a
/// custom protocol or WireGuard needs an Apple packet tunnel extension or an Android
/// <c>VpnService</c>, which this library does not provide.</para>
/// <para>Check <see cref="Capabilities"/> before offering an option; anything the platform lacks throws
/// <see cref="VpnNotSupportedException"/>.</para>
/// </remarks>
public interface IVpnManager
{
    /// <summary>What this platform can actually do. Check before calling.</summary>
    VpnCapabilities Capabilities { get; }

    /// <summary>
    /// Installs or replaces this app's VPN profile.
    /// </summary>
    /// <remarks>
    /// <para>The first save shows the OS consent prompt ("Allow ... to add VPN configurations" on Apple,
    /// the VPN connection request dialog on Android). A user who declines returns
    /// <see cref="AccessState.Denied"/> and nothing is saved - so this needs a foreground app.</para>
    /// <para>Saving does not connect. If the tunnel is up it keeps running on the old settings until the
    /// next <see cref="Connect"/>.</para>
    /// </remarks>
    /// <returns><see cref="AccessState.Available"/> once saved, <see cref="AccessState.Denied"/> if the user declined.</returns>
    Task<AccessState> SaveProfile(VpnProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Disconnects and deletes this app's VPN profile and any secrets saved with it. Does nothing if
    /// there is none.
    /// </summary>
    Task RemoveProfile(CancellationToken ct = default);

    /// <summary>
    /// Brings the tunnel up and waits until it is connected.
    /// </summary>
    /// <remarks>
    /// Returns straight away if it is already connected. Where the platform lacks
    /// <see cref="VpnCapabilities.Status"/> (Android 11 and 12) it returns as soon as the OS accepts the
    /// request, because there is nothing to wait on.
    /// </remarks>
    /// <param name="timeout">How long to wait for the tunnel. Defaults to 30 seconds.</param>
    /// <exception cref="VpnConnectionException">The tunnel failed or did not come up in time - the message carries the OS's reason where it gives one.</exception>
    /// <exception cref="VpnPermissionException">No profile has been saved, or the user withdrew consent.</exception>
    Task Connect(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Takes the tunnel down. The profile stays saved. Does nothing if it is not connected.
    /// </summary>
    Task Disconnect(CancellationToken ct = default);

    /// <summary>
    /// Reads the tunnel's current state, or <see cref="VpnConnectionState.Unknown"/> without
    /// <see cref="VpnCapabilities.Status"/>.
    /// </summary>
    Task<VpnConnectionState> GetStatus(CancellationToken ct = default);

    /// <summary>
    /// Fires when the tunnel's state changes - including changes made by the user in system settings or
    /// by the OS reconnecting on its own.
    /// </summary>
    /// <remarks>
    /// Subscribing delivers the current state once, so a subscriber does not sit blind until the next
    /// change. Never fires without <see cref="VpnCapabilities.Status"/>.
    /// </remarks>
    event EventHandler<VpnConnectionState>? StatusChanged;
}
