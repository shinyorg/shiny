using Microsoft.Extensions.Logging;

namespace Shiny.Net.Wifi;


/// <summary>
/// Shared plumbing for the platform <see cref="IWifiManager"/> implementations - subscription
/// counting on <see cref="Changed"/>, change de-duplication, and a throwing default for every
/// operation a platform does not support.
/// </summary>
/// <remarks>
/// <para>The native watchers behind <see cref="Changed"/> are all chatty: Android's NetworkCallback
/// fires several times for one association, NWPathMonitor re-delivers on unrelated route changes,
/// and NetworkManager emits a PropertiesChanged per property. They are funnelled through
/// <see cref="RaiseChangedIfDifferent"/>, which re-reads the network and stays quiet unless
/// something the caller can see actually moved.</para>
/// <para>That re-read goes through <see cref="GetCurrentNetwork"/> - the same call a caller makes -
/// so a platform only has to get the read right once for both the poll and the event to be
/// correct. Reads are serialised, because an asynchronous one can otherwise finish out of order
/// and publish a stale network over a newer one.</para>
/// </remarks>
public abstract class AbstractWifiManager(ILogger logger) : IWifiManager
{
    readonly SemaphoreSlim readLock = new(1, 1);
    WifiNetworkInfo? lastKnown;
    int subscriberCount;

    event EventHandler<WifiNetworkInfo?>? changed;
    public event EventHandler<WifiNetworkInfo?>? Changed
    {
        add
        {
            this.changed += value;
            if (Interlocked.Increment(ref this.subscriberCount) == 1)
            {
                this.StartListening();

                // publish where we are now, so a subscriber does not sit blind until the network
                // next moves. Some watchers replay their state on registration and some do not -
                // doing it here makes the first delivery the same on every platform, and the
                // de-duplication collapses it with a replay that arrives alongside it
                this.RaiseChangedIfDifferent();
            }
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0)
            {
                this.StopListening();

                // the next subscriber starts from nothing rather than from whatever was current
                // when the last one left, which may be several networks ago
                this.lastKnown = null;
            }
        }
    }


    public abstract WifiCapabilities Capabilities { get; }
    public abstract Task<WifiNetworkInfo?> GetCurrentNetwork(CancellationToken ct = default);


    /// <summary>Hook up the native watcher. Called on the first subscription to <see cref="Changed"/>.</summary>
    protected abstract void StartListening();

    /// <summary>Tear the native watcher down. Called when the last subscriber goes away.</summary>
    protected abstract void StopListening();


    /// <summary>
    /// Re-reads the current network and raises <see cref="Changed"/> only if it differs from the
    /// last state seen. Call this from the native watcher.
    /// </summary>
    /// <remarks>
    /// Fire and forget by design - the native watchers that call it are callbacks on an OS queue
    /// with nothing to await them, and a read that fails is logged rather than thrown into a
    /// platform callback where it would take the process down.
    /// </remarks>
    protected void RaiseChangedIfDifferent()
        => _ = this.RaiseChangedIfDifferentAsync();


    async Task RaiseChangedIfDifferentAsync()
    {
        await this.readLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var current = await this.GetCurrentNetwork().ConfigureAwait(false);
            if (Equals(this.lastKnown, current))
                return;

            this.lastKnown = current;
            this.changed?.Invoke(this, current);
        }
        catch (Exception ex)
        {
            logger.WifiError(ex, "Could not read the current network after the platform reported a change");
        }
        finally
        {
            this.readLock.Release();
        }
    }


    public abstract Task<AccessState> RequestAccess(CancellationToken ct = default);


    public virtual Task<IReadOnlyList<WifiNetwork>> Scan(CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.Scan);

    public virtual Task<WifiNetworkInfo> Connect(WifiConnectionRequest request, CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.Connect);

    public virtual Task<WifiNetworkInfo> Connect(string knownNetworkId, CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.ConnectKnownNetwork);

    public virtual Task Disconnect(CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.Disconnect);

    public virtual Task<IReadOnlyList<KnownWifiNetwork>> GetKnownNetworks(CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.KnownNetworks);

    public virtual Task Forget(string knownNetworkId, CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.ForgetNetwork);

    public virtual Task<bool> GetRadioEnabled(CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.RadioState);

    public virtual Task SetRadioEnabled(bool enabled, CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.RadioToggle);


    /// <summary>
    /// Polls <see cref="GetCurrentNetwork"/> until the platform has assigned an address.
    /// </summary>
    /// <remarks>
    /// Every platform reports a join the moment association succeeds, which is before DHCP has run.
    /// Handing back a <see cref="WifiNetworkInfo"/> with no address on it would be technically
    /// accurate and useless, so the connect paths wait here instead.
    /// </remarks>
    protected async Task<WifiNetworkInfo> WaitForAddress(string ssid, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            while (true)
            {
                var current = await this.GetCurrentNetwork(cts.Token).ConfigureAwait(false);
                if (current?.IpAddresses.Count > 0)
                    return current;

                await Task.Delay(500, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // the linked token tripped on our own timeout rather than the caller cancelling
            throw new WifiConnectionException($"Joined '{ssid}' but no address was assigned within {timeout}");
        }
    }


    /// <summary>
    /// The generic "this platform has no API for that" failure. Override the operation and throw a
    /// more specific message wherever there is something useful to tell the caller.
    /// </summary>
    protected WifiNotSupportedException NotSupported(WifiCapabilities capability)
        => WifiNotSupportedException.For(capability, "the operating system exposes no API for it to third-party apps");
}
