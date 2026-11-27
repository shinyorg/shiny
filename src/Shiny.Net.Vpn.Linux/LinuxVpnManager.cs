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
    readonly INmVpnWatchSource? watchOverride;
    readonly ConcurrentDictionary<string, string> failures = new();
    readonly string uuid = StrongSwanProfile.GetConnectionUuid(
        Assembly.GetEntryAssembly()?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName
    );
    Task? listening;
    CancellationTokenSource? listenCancel;
    volatile string? activePath;


    /// <summary>Watches added on a connection that is not a real bus - tests only.</summary>
    internal LinuxVpnManager(ILogger<LinuxVpnManager> logger, INmVpnWatchSource watches) : this(logger)
        => this.watchOverride = watches;


    INmVpnWatchSource Watches => this.watchOverride ?? this.client;


    /// <summary>The first wait before subscribing again after the bus dropped; doubles per failed attempt.</summary>
    internal TimeSpan ResubscribeDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>The longest wait between attempts to subscribe again while the bus is unavailable.</summary>
    internal TimeSpan MaxResubscribeDelay { get; set; } = TimeSpan.FromSeconds(30);


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
    {
        var cancel = new CancellationTokenSource();
        var firstAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        this.listenCancel = cancel;
        this.listening = firstAttempt.Task;
        _ = this.Listen(firstAttempt, cancel);
    }


    protected override void StopListening()
    {
        // the loop owns the source and disposes it once it has seen the cancellation
        var cancel = Interlocked.Exchange(ref this.listenCancel, null);
        this.listening = null;
        cancel?.Cancel();
    }


    /// <summary>
    /// Keeps the two watches alive for as long as anyone listens. A match rule dies with its D-Bus
    /// connection, so when the bus drops the dead handles are disposed and the watches added again on
    /// a new connection - backing off from <see cref="ResubscribeDelay"/> to <see cref="MaxResubscribeDelay"/>
    /// while the bus is unavailable - and the state is re-read once, because a change may have been
    /// missed in between.
    /// </summary>
    async Task Listen(TaskCompletionSource firstAttempt, CancellationTokenSource cancel)
    {
        var ct = cancel.Token;
        var delay = this.ResubscribeDelay;
        var resubscribing = false;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var watch = await this.TryWatch(ct).ConfigureAwait(false);
                firstAttempt.TrySetResult();

                if (watch != null)
                {
                    delay = this.ResubscribeDelay;
                    if (resubscribing)
                        this.RaiseStatusIfChanged();

                    resubscribing = true;
                    await this.WaitForLoss(watch, ct).ConfigureAwait(false);
                }
                await Task.Delay(delay, ct).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, this.MaxResubscribeDelay.Ticks));
            }
        }
        catch (OperationCanceledException)
        {
            // StopListening
        }
        finally
        {
            firstAttempt.TrySetResult();
            cancel.Dispose();
        }
    }


    async Task<NmWatch?> TryWatch(CancellationToken ct)
    {
        try
        {
            var watch = await this.Watches.Watch(this.RaiseStatusIfChanged, this.OnVpnStateChanged, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested)
            {
                // StopListening ran while the subscription was in flight
                watch.Dispose();
                return null;
            }
            logger.WatcherStarted("NetworkManager PropertiesChanged + VpnStateChanged");
            return watch;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.VpnError(ex, "Could not watch NetworkManager for VPN changes - retrying");
            return null;
        }
    }


    /// <summary>Returns once the watch's connection drops or listening stops, with the dead handles disposed either way.</summary>
    async Task WaitForLoss(NmWatch watch, CancellationToken ct)
    {
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = ct.Register(static x => ((TaskCompletionSource)x!).TrySetResult(), stopped);
        try
        {
            var first = await Task.WhenAny(watch.Lost, stopped.Task).ConfigureAwait(false);
            if (first == watch.Lost)
                logger.VpnError(watch.Lost.Result ?? new VpnException("The D-Bus connection closed"), "Lost the D-Bus connection - watching NetworkManager again");
        }
        finally
        {
            watch.Dispose();
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
