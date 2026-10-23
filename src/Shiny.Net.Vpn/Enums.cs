namespace Shiny.Net.Vpn;


/// <summary>
/// The VPN operations the current platform can actually perform.
/// </summary>
/// <remarks>
/// <para>Every platform Shiny targets has an IKEv2 client built into the OS that an app can configure
/// and drive, which is what this library manages - but they do not expose the same amount of it.
/// Android cannot report the connection state before API 33 and always uses the server address as the
/// remote identity, iOS cannot be handed a custom server root certificate, and NetworkManager's
/// strongSwan plugin reads certificates from files rather than taking them inline.</para>
/// <para>Each manager reports what it can do here and throws <see cref="VpnNotSupportedException"/>,
/// naming the specific limit, for the rest. Check the flag before offering the option in your UI.</para>
/// </remarks>
[Flags]
public enum VpnCapabilities
{
    /// <summary>Nothing is available - the OS has no VPN API this library can drive.</summary>
    None = 0,

    /// <summary>
    /// <see cref="IVpnManager.SaveProfile"/> and <see cref="IVpnManager.RemoveProfile"/> can install
    /// and delete this app's VPN profile.
    /// </summary>
    Profile = 1,

    /// <summary><see cref="IVpnManager.Connect"/> can bring the tunnel up.</summary>
    Connect = 2,

    /// <summary><see cref="IVpnManager.Disconnect"/> can take the tunnel down.</summary>
    Disconnect = 4,

    /// <summary>
    /// <see cref="IVpnManager.GetStatus"/> and <see cref="IVpnManager.StatusChanged"/> report the
    /// tunnel's state.
    /// </summary>
    /// <remarks>
    /// Without this flag <see cref="IVpnManager.Connect"/> returns as soon as the OS has accepted the
    /// request, because there is no way to tell when - or whether - the tunnel came up.
    /// </remarks>
    Status = 8,

    /// <summary>A <see cref="SharedSecretAuthentication"/> (IKEv2 pre-shared key) profile can be saved.</summary>
    SharedSecretAuthentication = 16,

    /// <summary>A <see cref="UsernamePasswordAuthentication"/> (IKEv2 EAP) profile can be saved.</summary>
    UsernamePasswordAuthentication = 32,

    /// <summary>A <see cref="CertificateAuthentication"/> (IKEv2 certificate) profile can be saved.</summary>
    CertificateAuthentication = 64,

    /// <summary>
    /// <see cref="VpnProfile.ServerRootCertificate"/> is honoured, so the server can present a
    /// certificate issued by a private CA.
    /// </summary>
    ServerRootCertificate = 128,

    /// <summary>
    /// <see cref="VpnProfile.RemoteIdentifier"/> can differ from <see cref="VpnProfile.ServerAddress"/>.
    /// </summary>
    RemoteIdentifier = 256
}


/// <summary>
/// The state of this app's VPN tunnel.
/// </summary>
public enum VpnConnectionState
{
    /// <summary>The platform cannot report the state - see <see cref="VpnCapabilities.Status"/>.</summary>
    Unknown,

    /// <summary>No profile has been saved, or it was removed - including by the user, from Settings.</summary>
    NotConfigured,

    /// <summary>A profile is saved and the tunnel is down.</summary>
    Disconnected,

    /// <summary>The tunnel is being negotiated.</summary>
    Connecting,

    /// <summary>The tunnel is up and traffic is flowing through it.</summary>
    Connected,

    /// <summary>The tunnel was up and is being re-established - after a network change, for example.</summary>
    Reconnecting,

    /// <summary>The tunnel is being torn down.</summary>
    Disconnecting
}
