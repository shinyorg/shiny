using Foundation;
using Microsoft.Extensions.Logging;
using NetworkExtension;

namespace Shiny.Net.Vpn;


/// <summary>
/// iOS, Mac Catalyst and macOS personal VPN, over <c>NEVPNManager</c> with an IKEv2 protocol.
/// </summary>
/// <remarks>
/// <para>Needs the <c>Personal VPN</c> capability and the <c>com.apple.developer.networking.vpn.api</c>
/// entitlement (<c>allow-vpn</c>). Without it the save fails the same way a user declining does, so
/// a <see cref="AccessState.Denied"/> on a first run that never showed a prompt means the entitlement
/// is missing.</para>
/// <para>NEVPNManager only reflects the saved configuration after <c>loadFromPreferences</c>, and a
/// freshly saved one has to be loaded again before it will start - skipping either is what made the
/// original 2020 Shiny.Vpn fail every connect - so the configuration is reloaded before every save and
/// start.</para>
/// <para>Secrets are never handed to NetworkExtension directly: it takes keychain persistent
/// references, so the shared secret and password are written to this app's keychain first.</para>
/// </remarks>
public class AppleVpnManager(ILogger<AppleVpnManager> logger) : AbstractVpnManager(logger)
{
    static NEVpnManager Manager => NEVpnManager.SharedManager;

    readonly SemaphoreSlim loadLock = new(1, 1);
    bool loaded;
    NSObject? statusObserver;
    NSObject? configurationObserver;


    public override VpnCapabilities Capabilities =>
        VpnCapabilities.Profile |
        VpnCapabilities.Connect |
        VpnCapabilities.Disconnect |
        VpnCapabilities.Status |
        VpnCapabilities.SharedSecretAuthentication |
        VpnCapabilities.UsernamePasswordAuthentication |
        VpnCapabilities.CertificateAuthentication |
        VpnCapabilities.RemoteIdentifier;


    protected override VpnNotSupportedException NotSupported(VpnCapabilities capability) => capability switch
    {
        VpnCapabilities.ServerRootCertificate => VpnNotSupportedException.For(
            capability,
            "Apple platforms only trust a private CA the user installed through a configuration profile. Leave ServerRootCertificate null and have the server present a certificate from a public CA, or distribute the CA in a profile"
        ),
        _ => base.NotSupported(capability)
    };


    protected override async Task<AccessState> SaveProfileCore(VpnProfile profile, CancellationToken ct)
    {
        await this.Load(true).ConfigureAwait(false);

        var protocol = new NEVpnProtocolIke2
        {
            ServerAddress = profile.ServerAddress,
            RemoteIdentifier = profile.RemoteIdentifier ?? profile.ServerAddress,
            LocalIdentifier = profile.LocalIdentifier,
            DisconnectOnSleep = false
        };

        switch (profile.Authentication)
        {
            case SharedSecretAuthentication psk:
                protocol.AuthenticationMethod = NEVpnIkeAuthenticationMethod.SharedSecret;
                protocol.SharedSecretReference = AppleVpnKeychain.Store(AppleVpnKeychain.SharedSecretAccount, psk.Secret);
                AppleVpnKeychain.Delete(AppleVpnKeychain.PasswordAccount);
                break;

            case UsernamePasswordAuthentication eap:
                // EAP rides on top of IKEv2 with no IKE-level authentication of the client
                protocol.AuthenticationMethod = NEVpnIkeAuthenticationMethod.None;
                protocol.UseExtendedAuthentication = true;
                protocol.Username = eap.Username;
                protocol.PasswordReference = AppleVpnKeychain.Store(AppleVpnKeychain.PasswordAccount, eap.Password);
                protocol.LocalIdentifier = profile.LocalIdentifier ?? eap.Username;
                AppleVpnKeychain.Delete(AppleVpnKeychain.SharedSecretAccount);
                break;

            case CertificateAuthentication cert:
                protocol.AuthenticationMethod = NEVpnIkeAuthenticationMethod.Certificate;
                protocol.IdentityData = NSData.FromArray(cert.Pkcs12);
                protocol.IdentityDataPassword = cert.Password;
                AppleVpnKeychain.Delete(AppleVpnKeychain.SharedSecretAccount);
                AppleVpnKeychain.Delete(AppleVpnKeychain.PasswordAccount);
                break;
        }

        Manager.ProtocolConfiguration = protocol;
        Manager.LocalizedDescription = profile.Name;
        Manager.Enabled = true;

        try
        {
            await Manager.SaveToPreferencesAsync().ConfigureAwait(false);
        }
        catch (NSErrorException ex) when (IsVpnError(ex.Error, NEVpnError.ConfigurationReadWriteFailed))
        {
            // the user tapped Don't Allow - or the entitlement is missing, which iOS reports identically
            return AccessState.Denied;
        }
        catch (NSErrorException ex)
        {
            throw new VpnException("The VPN profile could not be saved - " + ex.Error.LocalizedDescription, ex);
        }

        // a configuration only starts once it has been read back after the save
        await this.Load(true).ConfigureAwait(false);
        return AccessState.Available;
    }


    protected override async Task RemoveProfileCore(CancellationToken ct)
    {
        await this.Load(true).ConfigureAwait(false);
        if (Manager.Connection.Status != NEVpnStatus.Invalid)
        {
            Manager.Connection.StopVpnTunnel();
            try
            {
                await Manager.RemoveFromPreferencesAsync().ConfigureAwait(false);
            }
            catch (NSErrorException ex)
            {
                throw new VpnException("The VPN profile could not be removed - " + ex.Error.LocalizedDescription, ex);
            }
        }
        AppleVpnKeychain.Delete(AppleVpnKeychain.SharedSecretAccount);
        AppleVpnKeychain.Delete(AppleVpnKeychain.PasswordAccount);

        await this.Load(true).ConfigureAwait(false);
    }


    protected override async Task StartTunnel(CancellationToken ct)
    {
        await this.Load(true).ConfigureAwait(false);

        // the user can switch the configuration off in Settings - starting a disabled one fails outright
        if (!Manager.Enabled)
        {
            Manager.Enabled = true;
            await Manager.SaveToPreferencesAsync().ConfigureAwait(false);
            await this.Load(true).ConfigureAwait(false);
        }

        if (!Manager.Connection.StartVpnTunnel(out var error))
        {
            var message = error?.LocalizedDescription ?? "the system gave no reason";
            if (error != null && IsVpnError(error, NEVpnError.ConfigurationInvalid))
                throw new VpnPermissionException("No valid VPN profile is saved - call SaveProfile first: " + message);

            throw new VpnConnectionException("The VPN could not be started - " + message);
        }
    }


    protected override Task StopTunnel(CancellationToken ct)
    {
        Manager.Connection.StopVpnTunnel();
        return Task.CompletedTask;
    }


    protected override async Task<VpnConnectionState> GetStatusCore(CancellationToken ct)
    {
        await this.Load(false).ConfigureAwait(false);
        return Manager.Connection.Status switch
        {
            NEVpnStatus.Invalid => VpnConnectionState.NotConfigured,
            NEVpnStatus.Disconnected => VpnConnectionState.Disconnected,
            NEVpnStatus.Connecting => VpnConnectionState.Connecting,
            NEVpnStatus.Connected => VpnConnectionState.Connected,
            NEVpnStatus.Reasserting => VpnConnectionState.Reconnecting,
            NEVpnStatus.Disconnecting => VpnConnectionState.Disconnecting,
            _ => VpnConnectionState.Unknown
        };
    }


    protected override void StartListening()
    {
        this.statusObserver = NEVpnConnection.Notifications.ObserveStatusDidChange((_, _) => this.RaiseStatusIfChanged());

        // the user editing or deleting the configuration in Settings arrives here, not as a status change
        this.configurationObserver = NEVpnManager.Notifications.ObserveConfigurationChange((_, _) => _ = this.ReloadAndRaise());
        logger.WatcherStarted("NEVPNStatusDidChange");
    }


    protected override void StopListening()
    {
        this.statusObserver?.Dispose();
        this.statusObserver = null;
        this.configurationObserver?.Dispose();
        this.configurationObserver = null;
    }


    /// <remarks>
    /// <c>fetchLastDisconnectError</c> (iOS 16, macOS 13) is the only place Apple says why a tunnel
    /// failed - authentication, an unreachable server, a rejected certificate. Earlier versions give
    /// no reason at all.
    /// </remarks>
    protected override async Task<string?> GetFailureReason(CancellationToken ct)
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(16) &&
            !OperatingSystem.IsMacCatalystVersionAtLeast(16) &&
            !OperatingSystem.IsMacOSVersionAtLeast(13))
            return null;

        var tcs = new TaskCompletionSource<NSError?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Manager.Connection.FetchLastDisconnectError(error => tcs.TrySetResult(error));
        var lastError = await tcs.Task.WaitAsync(ct).ConfigureAwait(false);

        return lastError?.LocalizedDescription;
    }


    async Task ReloadAndRaise()
    {
        try
        {
            await this.Load(true).ConfigureAwait(false);
            this.RaiseStatusIfChanged();
        }
        catch (Exception ex)
        {
            logger.VpnError(ex, "Could not reload the VPN configuration after it changed");
        }
    }


    async Task Load(bool force)
    {
        if (this.loaded && !force)
            return;

        await this.loadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!this.loaded || force)
            {
                await Manager.LoadFromPreferencesAsync().ConfigureAwait(false);
                this.loaded = true;
            }
        }
        catch (NSErrorException ex)
        {
            throw new VpnException("The VPN configuration could not be loaded - " + ex.Error.LocalizedDescription, ex);
        }
        finally
        {
            this.loadLock.Release();
        }
    }


    static bool IsVpnError(NSError error, NEVpnError code)
        => error.Domain == NEVpnError.ConfigurationInvalid.GetDomain()?.ToString() && error.Code == (nint)code;
}
