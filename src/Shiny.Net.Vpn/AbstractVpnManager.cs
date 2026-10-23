using Microsoft.Extensions.Logging;

namespace Shiny.Net.Vpn;


/// <summary>
/// Shared plumbing for the platform <see cref="IVpnManager"/> implementations - profile validation
/// against <see cref="Capabilities"/>, waiting for a tunnel to come up, and subscription counting and
/// de-duplication on <see cref="StatusChanged"/>.
/// </summary>
/// <remarks>
/// <para>Platforms only translate: they read the native state, start and stop the native tunnel, and
/// call <see cref="RaiseStatusIfChanged"/> from whatever the OS notifies them with. Deciding whether a
/// connect attempt succeeded lives here, so it behaves the same everywhere.</para>
/// <para>Connecting is judged by polling <see cref="GetStatus"/> rather than by the native
/// notifications alone. Every OS reports the transitions, but none of them promises to report each
/// one, and a missed "connected" would otherwise hang the caller until the timeout.</para>
/// </remarks>
public abstract class AbstractVpnManager(ILogger logger) : IVpnManager
{
    static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(30);
    static readonly TimeSpan DisconnectWait = TimeSpan.FromSeconds(10);

    readonly SemaphoreSlim readLock = new(1, 1);
    VpnConnectionState? lastKnown;
    int subscriberCount;
    volatile string? failureReason;


    /// <summary>How often a connect or disconnect re-reads the state while it waits.</summary>
    internal TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(500);


    event EventHandler<VpnConnectionState>? changed;
    public event EventHandler<VpnConnectionState>? StatusChanged
    {
        add
        {
            this.changed += value;
            if (this.Capabilities.HasFlag(VpnCapabilities.Status) && Interlocked.Increment(ref this.subscriberCount) == 1)
            {
                this.StartListening();

                // publish where we are now so a subscriber does not sit blind until the tunnel next moves
                this.RaiseStatusIfChanged();
            }
        }
        remove
        {
            this.changed -= value;
            if (this.Capabilities.HasFlag(VpnCapabilities.Status) && Interlocked.Decrement(ref this.subscriberCount) == 0)
            {
                this.StopListening();

                // the next subscriber starts from nothing rather than a state that may be several changes old
                this.lastKnown = null;
            }
        }
    }


    public abstract VpnCapabilities Capabilities { get; }


    /// <summary>
    /// The reason the OS gave for the last failure, set by the platform from its error callback.
    /// <see cref="Connect"/> clears it before starting and reports it if the attempt fails.
    /// </summary>
    protected string? FailureReason
    {
        get => this.failureReason;
        set => this.failureReason = value;
    }


    public async Task<AccessState> SaveProfile(VpnProfile profile, CancellationToken ct = default)
    {
        this.EnsureCanSave(profile);
        logger.SavingProfile(profile.Name, profile.ServerAddress, profile.Authentication.ToString());

        var result = await this.SaveProfileCore(profile, ct).ConfigureAwait(false);
        if (result != AccessState.Available)
            logger.ProfileNotSaved(result);

        return result;
    }


    public Task RemoveProfile(CancellationToken ct = default)
    {
        this.Require(VpnCapabilities.Profile);
        logger.RemovingProfile();
        return this.RemoveProfileCore(ct);
    }


    public async Task Connect(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        this.Require(VpnCapabilities.Connect);

        if (!this.Capabilities.HasFlag(VpnCapabilities.Status))
        {
            // nothing to wait on - the OS took the request and that is all this platform will say
            this.FailureReason = null;
            await this.StartTunnel(ct).ConfigureAwait(false);
            logger.ConnectRequested();
            return;
        }

        var current = await this.GetStatusCore(ct).ConfigureAwait(false);
        if (current == VpnConnectionState.Connected)
            return;

        if (current == VpnConnectionState.NotConfigured)
            throw new VpnPermissionException("No VPN profile is saved - call SaveProfile first, or the user removed it from the system VPN settings");

        // keeps the platform watchers up for the attempt, so an error the OS only reports through them
        // (Android's VPN manager events, NetworkManager's VpnStateChanged) is caught into FailureReason
        EventHandler<VpnConnectionState> keepAlive = static (_, _) => { };
        this.StatusChanged += keepAlive;
        try
        {
            this.FailureReason = null;
            logger.Connecting();
            await this.StartTunnel(ct).ConfigureAwait(false);
            await this.WaitForConnected(timeout ?? DefaultConnectTimeout, ct).ConfigureAwait(false);
            logger.Connected();
        }
        finally
        {
            this.StatusChanged -= keepAlive;
        }
    }


    public async Task Disconnect(CancellationToken ct = default)
    {
        this.Require(VpnCapabilities.Disconnect);
        await this.StopTunnel(ct).ConfigureAwait(false);
        logger.Disconnected();

        if (this.Capabilities.HasFlag(VpnCapabilities.Status))
            await this.WaitForDisconnected(ct).ConfigureAwait(false);
    }


    public Task<VpnConnectionState> GetStatus(CancellationToken ct = default)
        => this.Capabilities.HasFlag(VpnCapabilities.Status)
            ? this.GetStatusCore(ct)
            : Task.FromResult(VpnConnectionState.Unknown);


    /// <summary>
    /// Re-reads the state and raises <see cref="StatusChanged"/> only if it differs from the last one
    /// seen. Call this from the native watcher.
    /// </summary>
    /// <remarks>
    /// Fire and forget - the watchers that call it are OS callbacks with nothing to await them, and a
    /// read that fails is logged rather than thrown into a platform callback.
    /// </remarks>
    protected void RaiseStatusIfChanged()
        => _ = this.RaiseStatusIfChangedAsync();


    async Task RaiseStatusIfChangedAsync()
    {
        await this.readLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var current = await this.GetStatusCore(CancellationToken.None).ConfigureAwait(false);
            if (this.lastKnown == current)
                return;

            this.lastKnown = current;
            this.changed?.Invoke(this, current);
        }
        catch (Exception ex)
        {
            logger.VpnError(ex, "Could not read the VPN state after the platform reported a change");
        }
        finally
        {
            this.readLock.Release();
        }
    }


    /// <remarks>
    /// The state right after the start call is not trustworthy on its own: the OS takes a moment to move
    /// off <see cref="VpnConnectionState.Disconnected"/>, so a disconnected read only counts as a failure
    /// once the attempt has been seen in progress - or once the platform has reported a reason.
    /// </remarks>
    async Task WaitForConnected(TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        var started = false;
        try
        {
            while (true)
            {
                var state = await this.GetStatusCore(cts.Token).ConfigureAwait(false);
                switch (state)
                {
                    case VpnConnectionState.Connected:
                        return;

                    case VpnConnectionState.Connecting:
                    case VpnConnectionState.Reconnecting:
                        started = true;
                        break;

                    case VpnConnectionState.Disconnected:
                    case VpnConnectionState.NotConfigured:
                        if (started || this.FailureReason != null)
                        {
                            var reason = await this.GetFailureReason(ct).ConfigureAwait(false);
                            throw new VpnConnectionException("The VPN failed to connect - " + (reason ?? "the operating system gave no reason"));
                        }
                        break;
                }
                await Task.Delay(this.PollInterval, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // our own timeout rather than the caller cancelling - leave the half-open attempt behind no further
            await this.StopTunnel(CancellationToken.None).ConfigureAwait(false);

            var reason = await this.GetFailureReason(CancellationToken.None).ConfigureAwait(false);
            throw new VpnConnectionException(
                $"The VPN did not connect within {timeout}" + (reason == null ? String.Empty : " - " + reason)
            );
        }
    }


    /// <remarks>
    /// Best effort: a tunnel that will not report down within the wait is the OS's to finish tearing
    /// down, not a failure of the call. Waiting at all is what lets a Connect straight after a
    /// Disconnect start from a settled state.
    /// </remarks>
    async Task WaitForDisconnected(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(DisconnectWait);
        try
        {
            while (true)
            {
                var state = await this.GetStatusCore(cts.Token).ConfigureAwait(false);
                if (state is VpnConnectionState.Disconnected or VpnConnectionState.NotConfigured)
                    return;

                await Task.Delay(this.PollInterval, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.DisconnectSlow(DisconnectWait);
        }
    }


    void EnsureCanSave(VpnProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.Name, nameof(VpnProfile.Name));
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.ServerAddress, nameof(VpnProfile.ServerAddress));
        ArgumentNullException.ThrowIfNull(profile.Authentication, nameof(VpnProfile.Authentication));

        this.Require(VpnCapabilities.Profile);
        this.Require(profile.Authentication.RequiredCapability);

        if (profile.ServerRootCertificate != null)
            this.Require(VpnCapabilities.ServerRootCertificate);

        if (profile.RemoteIdentifier != null && !profile.RemoteIdentifier.Equals(profile.ServerAddress, StringComparison.OrdinalIgnoreCase))
            this.Require(VpnCapabilities.RemoteIdentifier);
    }


    void Require(VpnCapabilities capability)
    {
        if (!this.Capabilities.HasFlag(capability))
            throw this.NotSupported(capability);
    }


    /// <summary>Installs the validated profile. Return <see cref="AccessState.Denied"/> if the user declined.</summary>
    protected abstract Task<AccessState> SaveProfileCore(VpnProfile profile, CancellationToken ct);

    /// <summary>Disconnects and deletes the profile and its secrets. Must not fail when there is none.</summary>
    protected abstract Task RemoveProfileCore(CancellationToken ct);

    /// <summary>Asks the OS to bring the tunnel up. Return once it has accepted - the waiting happens here.</summary>
    protected abstract Task StartTunnel(CancellationToken ct);

    /// <summary>Asks the OS to take the tunnel down. Must not fail when it is already down.</summary>
    protected abstract Task StopTunnel(CancellationToken ct);

    /// <summary>Reads the native state. Only called when the platform has <see cref="VpnCapabilities.Status"/>.</summary>
    protected abstract Task<VpnConnectionState> GetStatusCore(CancellationToken ct);

    /// <summary>Hook up the native watcher. Called on the first subscription to <see cref="StatusChanged"/>.</summary>
    protected abstract void StartListening();

    /// <summary>Tear the native watcher down. Called when the last subscriber goes away.</summary>
    protected abstract void StopListening();


    /// <summary>
    /// Why the last attempt failed, for the exception <see cref="Connect"/> throws. Defaults to
    /// <see cref="FailureReason"/>; override where the OS has to be asked instead.
    /// </summary>
    protected virtual Task<string?> GetFailureReason(CancellationToken ct)
        => Task.FromResult(this.FailureReason);


    /// <summary>
    /// The generic "this platform cannot" failure. Override where there is something more specific to
    /// tell the caller.
    /// </summary>
    protected virtual VpnNotSupportedException NotSupported(VpnCapabilities capability)
        => VpnNotSupportedException.For(capability, "the operating system's VPN API does not offer it to apps");
}
