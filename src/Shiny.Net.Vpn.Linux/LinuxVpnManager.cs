using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Shiny.Net.Vpn.NetworkManager;
using Tmds.DBus.Protocol;

namespace Shiny.Net.Vpn;


/// <summary>
/// Linux VPN over NetworkManager's strongSwan plugin, driven through D-Bus.
/// </summary>
/// <remarks>
/// <para>Needs a running NetworkManager with the strongSwan plugin installed
/// (<c>network-manager-strongswan</c> on Debian/Ubuntu, <c>NetworkManager-strongswan</c> on Fedora).
/// Saving, removing and connecting go through polkit - interactive on a desktop, and in a headless
/// session needing a rule for <c>org.freedesktop.NetworkManager.settings.modify.system</c> and
/// <c>org.freedesktop.NetworkManager.network-control</c>.</para>
/// <para>The app's profile is the NetworkManager connection with a UUID derived from the entry
/// assembly's name, so it is found again on the next launch with nothing persisted - and shows in the
/// desktop's network settings under <see cref="VpnProfile.Name"/>.</para>
/// <para>Certificate authentication is not offered: the plugin only reads client keys from files, and
/// writing an unencrypted private key to disk to satisfy it is not something to do quietly.</para>
/// </remarks>
public class LinuxVpnManager(ILogger<LinuxVpnManager> logger) : AbstractVpnManager(logger), IAsyncDisposable
{
    readonly NmVpnClient client = new();
    readonly ConcurrentDictionary<string, string> failures = new();
    readonly string uuid = StrongSwanProfile.GetConnectionUuid(
        Assembly.GetEntryAssembly()?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName
    );
    Task<IDisposable[]>? listening;
    volatile string? activePath;


    public override VpnCapabilities Capabilities =>
        VpnCapabilities.Profile |
        VpnCapabilities.Connect |
        VpnCapabilities.Disconnect |
        VpnCapabilities.Status |
        VpnCapabilities.SharedSecretAuthentication |
        VpnCapabilities.UsernamePasswordAuthentication |
        VpnCapabilities.ServerRootCertificate |
        VpnCapabilities.RemoteIdentifier;


    protected override VpnNotSupportedException NotSupported(VpnCapabilities capability) => capability switch
    {
        VpnCapabilities.CertificateAuthentication => VpnNotSupportedException.For(
            capability,
            "NetworkManager's strongSwan plugin only reads client certificates and keys from files on disk. Use a shared secret or username/password profile on Linux"
        ),
        _ => base.NotSupported(capability)
    };


    string ServerCaPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "shiny",
        "vpn",
        this.uuid + ".pem"
    );


    protected override async Task<AccessState> SaveProfileCore(VpnProfile profile, CancellationToken ct)
    {
        if (StrongSwanProfile.IsPluginInstalled() == false)
            throw new VpnException("NetworkManager's strongSwan plugin is not installed - install network-manager-strongswan (Debian/Ubuntu) or NetworkManager-strongswan (Fedora)");

        string? caPath = null;
        if (profile.ServerRootCertificate != null)
        {
            caPath = this.ServerCaPath;
            Directory.CreateDirectory(Path.GetDirectoryName(caPath)!);
            await File.WriteAllTextAsync(caPath, StrongSwanProfile.ToPem(profile.ServerRootCertificate), ct).ConfigureAwait(false);
        }
        var settings = StrongSwanProfile.Build(profile, this.uuid, caPath);

        try
        {
            var existing = await this.client.FindConnection(this.uuid, ct).ConfigureAwait(false);
            if (existing == null)
                await this.client.AddConnection(settings, ct).ConfigureAwait(false);
            else
                await this.client.UpdateConnection(existing, settings, ct).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException ex) when (IsPermissionDenied(ex))
        {
            // polkit said no - the user cancelled the authentication dialog, or no rule allows it headless
            return AccessState.Denied;
        }
        catch (DBusExceptionBase ex)
        {
            throw new VpnException("NetworkManager refused the VPN profile - " + Describe(ex), ex);
        }
        if (caPath == null)
            DeleteFile(this.ServerCaPath);

        return AccessState.Available;
    }


    protected override async Task RemoveProfileCore(CancellationToken ct)
    {
        try
        {
            var active = await this.client.FindActive(this.uuid, ct).ConfigureAwait(false);
            if (active != null)
                await this.client.Deactivate(active.Path, ct).ConfigureAwait(false);

            var existing = await this.client.FindConnection(this.uuid, ct).ConfigureAwait(false);
            if (existing != null)
                await this.client.DeleteConnection(existing, ct).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException ex) when (IsPermissionDenied(ex))
        {
            throw new VpnPermissionException("NetworkManager (polkit) refused to remove the VPN profile - " + Describe(ex), ex);
        }
        catch (DBusExceptionBase ex)
        {
            throw new VpnException("The VPN profile could not be removed - " + Describe(ex), ex);
        }
        DeleteFile(this.ServerCaPath);
    }


    protected override async Task StartTunnel(CancellationToken ct)
    {
        // the failure signal must be subscribed before activating - a refused login can fail the
        // connection faster than the first poll, and the reason only travels in that signal
        if (this.listening != null)
            await this.listening.ConfigureAwait(false);

        var connection = await this.client.FindConnection(this.uuid, ct).ConfigureAwait(false)
            ?? throw new VpnPermissionException("No VPN profile is saved - call SaveProfile first, or it was deleted from the network settings");

        this.failures.Clear();
        try
        {
            var path = await this.client.Activate(connection, ct).ConfigureAwait(false);
            this.activePath = path;

            if (this.failures.TryGetValue(path, out var reason))
                this.FailureReason = reason;
        }
        catch (DBusErrorReplyException ex) when (IsPermissionDenied(ex))
        {
            throw new VpnPermissionException("NetworkManager (polkit) refused to start the VPN - " + Describe(ex), ex);
        }
        catch (DBusExceptionBase ex)
        {
            throw new VpnConnectionException("NetworkManager could not start the VPN - " + Describe(ex), ex);
        }
    }


    protected override async Task StopTunnel(CancellationToken ct)
    {
        try
        {
            var active = await this.client.FindActive(this.uuid, ct).ConfigureAwait(false);
            if (active != null)
                await this.client.Deactivate(active.Path, ct).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException ex) when (IsPermissionDenied(ex))
        {
            throw new VpnPermissionException("NetworkManager (polkit) refused to stop the VPN - " + Describe(ex), ex);
        }
        catch (DBusErrorReplyException)
        {
            // went down on its own between the lookup and the call
        }
    }


    protected override async Task<VpnConnectionState> GetStatusCore(CancellationToken ct)
    {
        var active = await this.client.FindActive(this.uuid, ct).ConfigureAwait(false);
        if (active != null)
        {
            return active.State switch
            {
                NmConstants.ActiveStateActivating => VpnConnectionState.Connecting,
                NmConstants.ActiveStateActivated => VpnConnectionState.Connected,
                NmConstants.ActiveStateDeactivating => VpnConnectionState.Disconnecting,
                NmConstants.ActiveStateDeactivated => VpnConnectionState.Disconnected,
                _ => VpnConnectionState.Unknown
            };
        }

        var saved = await this.client.FindConnection(this.uuid, ct).ConfigureAwait(false);
        return saved == null ? VpnConnectionState.NotConfigured : VpnConnectionState.Disconnected;
    }


    protected override void StartListening()
        => this.listening = this.Listen();


    protected override void StopListening()
    {
        var current = this.listening;
        this.listening = null;
        if (current != null)
            _ = DisposeWhenReady(current);
    }


    async Task<IDisposable[]> Listen()
    {
        try
        {
            var properties = await this.client.WatchPropertiesChanged(this.RaiseStatusIfChanged).ConfigureAwait(false);
            var vpnState = await this.client.WatchVpnStateChanged(this.OnVpnStateChanged).ConfigureAwait(false);
            logger.WatcherStarted("NetworkManager PropertiesChanged + VpnStateChanged");
            return [properties, vpnState];
        }
        catch (Exception ex)
        {
            logger.VpnError(ex, "Could not watch NetworkManager for VPN changes");
            return [];
        }
    }


    /// <remarks>
    /// Failures are kept per active connection path because the signal can arrive before
    /// ActivateConnection has replied with the path - <see cref="StartTunnel"/> picks it up from there.
    /// </remarks>
    void OnVpnStateChanged(NmVpnStateChange change)
    {
        if (change.State == NmConstants.VpnStateFailed)
        {
            var reason = StrongSwanProfile.DescribeReason(change.Reason);
            this.failures[change.Path] = reason;

            if (change.Path == this.activePath)
            {
                this.FailureReason = reason;
                logger.PlatformFailure(reason);
            }
        }
        this.RaiseStatusIfChanged();
    }


    static async Task DisposeWhenReady(Task<IDisposable[]> subscriptions)
    {
        foreach (var subscription in await subscriptions.ConfigureAwait(false))
            subscription.Dispose();
    }


    static bool IsPermissionDenied(DBusErrorReplyException ex)
        => ex.ErrorName is NmConstants.SettingsPermissionDeniedError or NmConstants.PermissionDeniedError;


    static string Describe(DBusExceptionBase ex)
        => (ex as DBusErrorReplyException)?.ErrorMessage ?? ex.Message;


    static void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }


    public async ValueTask DisposeAsync()
    {
        this.StopListening();
        await this.client.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
