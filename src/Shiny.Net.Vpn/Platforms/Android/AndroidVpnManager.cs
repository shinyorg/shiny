using System.Runtime.Versioning;
using System.Text;
using Android.Content;
using Android.Net;
using Android.Runtime;
using Java.Security;
using Java.Security.Cert;
using Microsoft.Extensions.Logging;
using AndroidVpnManagerService = Android.Net.VpnManager;

namespace Shiny.Net.Vpn;


/// <summary>
/// Android's platform VPN profiles - the OS's own IKEv2 client, configured through
/// <c>VpnManager</c> and <c>Ikev2VpnProfile</c>.
/// </summary>
/// <remarks>
/// <para>This is not <c>VpnService</c>. A <c>VpnService</c> hands the app a TUN device and leaves it to
/// implement the protocol and move every packet itself; a provisioned profile is negotiated and run by
/// the OS, needs no running service in the app, and survives the app being closed.</para>
/// <para>Needs API 30 (Android 11) for profiles at all. Before API 33 the OS offers no way to read the
/// profile's state back, so <see cref="VpnCapabilities.Status"/> is absent and
/// <see cref="IVpnManager.Connect"/> returns once the start is accepted. From API 33 the state comes
/// from <c>getProvisionedVpnProfileState</c>, and failures arrive as <c>ACTION_VPN_MANAGER_EVENT</c>
/// broadcasts carrying the IKE or network error.</para>
/// <para>Android always uses <see cref="VpnProfile.ServerAddress"/> as the server's identity, and lists
/// the profile in Settings under the app's name rather than <see cref="VpnProfile.Name"/>.</para>
/// </remarks>
public class AndroidVpnManager(AndroidPlatform platform, ILogger<AndroidVpnManager> logger) : AbstractVpnManager(logger)
{
    VpnStateCallback? networkCallback;
    ActionBroadcastReceiver? eventReceiver;


    public override VpnCapabilities Capabilities
    {
        get
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(30))
                return VpnCapabilities.None;

            var caps =
                VpnCapabilities.Profile |
                VpnCapabilities.Connect |
                VpnCapabilities.Disconnect |
                VpnCapabilities.SharedSecretAuthentication |
                VpnCapabilities.UsernamePasswordAuthentication |
                VpnCapabilities.CertificateAuthentication |
                VpnCapabilities.ServerRootCertificate;

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
                caps |= VpnCapabilities.Status;

            return caps;
        }
    }


    protected override VpnNotSupportedException NotSupported(VpnCapabilities capability)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            return VpnNotSupportedException.For(capability, "platform VPN profiles arrived in Android 11 (API 30)");

        return capability switch
        {
            VpnCapabilities.Status => VpnNotSupportedException.For(capability, "Android only reports a provisioned profile's state from Android 13 (API 33)"),
            VpnCapabilities.RemoteIdentifier => VpnNotSupportedException.For(capability, "Ikev2VpnProfile always uses the server address as the server's identity - leave RemoteIdentifier null or equal to ServerAddress"),
            _ => base.NotSupported(capability)
        };
    }


    [SupportedOSPlatform("android30.0")]
    AndroidVpnManagerService Vpn => platform.GetSystemService<AndroidVpnManagerService>(Context.VpnManagementService);


    protected override async Task<AccessState> SaveProfileCore(VpnProfile profile, CancellationToken ct)
    {
        // Capabilities is None below API 30, so the base class has already refused
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            throw this.NotSupported(VpnCapabilities.Profile);

        var native = BuildProfile(profile);

        // null means consent was granted before and the profile is installed as it stands; an intent
        // is the system consent dialog, which installs nothing until the user accepts it
        var consent = this.Vpn.ProvisionVpnProfile(native);
        if (consent == null)
            return AccessState.Available;

        var result = await platform.StartActivityForResult(consent, ct).ConfigureAwait(false);
        return result.IsOk ? AccessState.Available : AccessState.Denied;
    }


    protected override Task RemoveProfileCore(CancellationToken ct)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            this.Vpn.DeleteProvisionedVpnProfile();

        return Task.CompletedTask;
    }


    protected override Task StartTunnel(CancellationToken ct)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            throw this.NotSupported(VpnCapabilities.Connect);

        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
                this.Vpn.StartProvisionedVpnProfileSession();
            else
                this.Vpn.StartProvisionedVpnProfile();
        }
        catch (Java.Lang.SecurityException ex)
        {
            throw new VpnPermissionException("No VPN profile is saved, or the user withdrew this app's VPN consent - call SaveProfile first", ex);
        }
        catch (Java.Lang.IllegalArgumentException ex)
        {
            throw new VpnPermissionException("No VPN profile is saved - call SaveProfile first", ex);
        }
        return Task.CompletedTask;
    }


    protected override Task StopTunnel(CancellationToken ct)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            this.Vpn.StopProvisionedVpnProfile();

        return Task.CompletedTask;
    }


    protected override Task<VpnConnectionState> GetStatusCore(CancellationToken ct)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return Task.FromResult(VpnConnectionState.Unknown);

        var state = this.Vpn.ProvisionedVpnProfileState;
        var result = state == null
            ? VpnConnectionState.NotConfigured
            : (VpnState)state.State switch
            {
                VpnState.Connected => VpnConnectionState.Connected,
                VpnState.Connecting => VpnConnectionState.Connecting,

                // FAILED is a stopped session with an error behind it - the error comes through the
                // VPN manager event, and the state is simply down
                VpnState.Failed => VpnConnectionState.Disconnected,
                _ => VpnConnectionState.Disconnected
            };

        return Task.FromResult(result);
    }


    /// <remarks>
    /// Two sources, because neither covers everything: the network callback sees the VPN network
    /// appear and disappear (connected and disconnected), while the VPN manager event is the only
    /// thing that says why a session failed or that the user turned it off in Settings.
    /// </remarks>
    protected override void StartListening()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return;

        var connectivity = platform.GetSystemService<ConnectivityManager>(Context.ConnectivityService);
        var request = new NetworkRequest.Builder()
            .AddTransportType(TransportType.Vpn)
            .RemoveCapability(NetCapability.NotVpn)
            .Build();

        this.networkCallback = new VpnStateCallback(this.RaiseStatusIfChanged);
        connectivity.RegisterNetworkCallback(request, this.networkCallback);

        this.eventReceiver = new ActionBroadcastReceiver { OnAction = this.OnVpnManagerEvent };
        var filter = new IntentFilter(AndroidVpnManagerService.ActionVpnManagerEvent);
        platform.AppContext.RegisterReceiver(this.eventReceiver, filter, ReceiverFlags.NotExported);

        logger.WatcherStarted("NetworkCallback + ACTION_VPN_MANAGER_EVENT");
    }


    protected override void StopListening()
    {
        if (this.networkCallback != null)
        {
            platform.GetSystemService<ConnectivityManager>(Context.ConnectivityService).UnregisterNetworkCallback(this.networkCallback);
            this.networkCallback = null;
        }
        if (this.eventReceiver != null)
        {
            platform.AppContext.UnregisterReceiver(this.eventReceiver);
            this.eventReceiver = null;
        }
    }


    void OnVpnManagerEvent(Intent intent)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return;

        var reason = DescribeEvent(intent);
        if (reason != null)
        {
            this.FailureReason = reason;
            logger.PlatformFailure(reason);
        }
        this.RaiseStatusIfChanged();
    }


    [SupportedOSPlatform("android33.0")]
    static string? DescribeEvent(Intent intent)
    {
        var categories = intent.Categories;
        if (categories == null)
            return null;

        var code = intent.GetIntExtra(AndroidVpnManagerService.ExtraErrorCode, -1);
        if (categories.Contains(AndroidVpnManagerService.CategoryEventDeactivatedByUser))
            return "the user turned the VPN off";

        if (categories.Contains(AndroidVpnManagerService.CategoryEventAlwaysOnStateChanged))
            return null;

        if (categories.Contains(AndroidVpnManagerService.CategoryEventIkeError))
            return code switch
            {
                // IkeProtocolException codes - the ones a misconfigured profile actually produces
                24 => "the server rejected the credentials (IKE AUTHENTICATION_FAILED)",
                14 => "the server rejected the proposed algorithms (IKE NO_PROPOSAL_CHOSEN)",
                7 => "the server rejected the request as malformed (IKE INVALID_SYNTAX)",
                _ => $"IKE negotiation failed (IKE error {code})"
            };

        if (categories.Contains(AndroidVpnManagerService.CategoryEventNetworkError))
            // VpnManager.ERROR_CODE_NETWORK_UNKNOWN_HOST, _PROTOCOL_TIMEOUT, _LOST and _IO
            return code switch
            {
                0 => "the server address could not be resolved",
                1 => "the server did not respond",
                2 => "the underlying network was lost",
                3 => "a network I/O error occurred",
                _ => $"a network error occurred (error {code})"
            };

        return null;
    }


    [SupportedOSPlatform("android30.0")]
    static Ikev2VpnProfile BuildProfile(VpnProfile profile)
    {
        var identity = profile.LocalIdentifier ?? (profile.Authentication as UsernamePasswordAuthentication)?.Username;
        if (String.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Android needs VpnProfile.LocalIdentifier for shared secret and certificate profiles - it has no default identity to fall back on", nameof(profile));

        var builder = new Ikev2VpnProfile.Builder(profile.ServerAddress, identity);
        var serverRoot = profile.ServerRootCertificate == null ? null : ReadCertificate(profile.ServerRootCertificate);

        switch (profile.Authentication)
        {
            case SharedSecretAuthentication psk:
                builder.SetAuthPsk(Encoding.UTF8.GetBytes(psk.Secret));
                break;

            case UsernamePasswordAuthentication eap:
                builder.SetAuthUsernamePassword(eap.Username, eap.Password, serverRoot);
                break;

            case CertificateAuthentication cert:
                var (clientCert, key) = ReadPkcs12(cert);
                builder.SetAuthDigitalSignature(clientCert, key, serverRoot);
                break;
        }
        return builder.Build();
    }


    static X509Certificate ReadCertificate(byte[] encoded)
    {
        try
        {
            // CertificateFactory accepts DER and PEM alike
            using var stream = new MemoryStream(encoded);
            var factory = CertificateFactory.GetInstance("X.509")!;
            return factory.GenerateCertificate(stream).JavaCast<X509Certificate>()!;
        }
        catch (Java.Lang.Exception ex)
        {
            throw new ArgumentException("ServerRootCertificate is not a DER or PEM encoded X.509 certificate", nameof(VpnProfile.ServerRootCertificate), ex);
        }
    }


    static (X509Certificate Certificate, IPrivateKey Key) ReadPkcs12(CertificateAuthentication cert)
    {
        var password = cert.Password?.ToCharArray() ?? [];
        try
        {
            var store = KeyStore.GetInstance("PKCS12")!;
            using var stream = new MemoryStream(cert.Pkcs12);
            store.Load(stream, password);

            var aliases = store.Aliases();
            while (aliases != null && aliases.HasMoreElements)
            {
                var alias = aliases.NextElement()?.ToString();
                if (alias != null && store.IsKeyEntry(alias))
                {
                    // Java interfaces need JavaCast - a C# cast on the peer wrapper fails
                    var key = store.GetKey(alias, password).JavaCast<IPrivateKey>();
                    var certificate = store.GetCertificate(alias).JavaCast<X509Certificate>();
                    if (key != null && certificate != null)
                        return (certificate, key);
                }
            }
        }
        catch (Java.Lang.Exception ex)
        {
            throw new ArgumentException("The PKCS#12 bundle could not be read - check the password", nameof(CertificateAuthentication.Pkcs12), ex);
        }
        throw new ArgumentException("The PKCS#12 bundle holds no private key with a certificate", nameof(CertificateAuthentication.Pkcs12));
    }
}


/// <summary>
/// Re-reads the profile state whenever a VPN network appears, changes or goes away.
/// </summary>
class VpnStateCallback(Action onChange) : ConnectivityManager.NetworkCallback
{
    public override void OnAvailable(Network network) => onChange();
    public override void OnLost(Network network) => onChange();
    public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities) => onChange();
}
