namespace Shiny.Net.Wifi;


/// <summary>
/// Shared plumbing for the platform <see cref="IWifiManager"/> implementations - subscription
/// counting on <see cref="Changed"/>, change de-duplication, and a throwing default for every
/// operation a platform does not support.
/// </summary>
/// <remarks>
/// The native watchers behind <see cref="Changed"/> are all chatty: Android's NetworkCallback fires
/// several times for one association, NWPathMonitor re-delivers on unrelated route changes, and
/// NetworkManager emits a PropertiesChanged per property. They are funnelled through
/// <see cref="RaiseChangedIfDifferent"/>, which compares against the last state and stays quiet
/// unless something the caller can see actually moved.
/// </remarks>
public abstract class AbstractWifiManager : IWifiManager
{
    readonly Lock stateLock = new();
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
                lock (this.stateLock)
                    this.lastKnown = this.CurrentNetwork;

                this.StartListening();
            }
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0)
                this.StopListening();
        }
    }


    public abstract WifiCapabilities Capabilities { get; }
    public abstract WifiNetworkInfo? CurrentNetwork { get; }


    /// <summary>Hook up the native watcher. Called on the first subscription to <see cref="Changed"/>.</summary>
    protected abstract void StartListening();

    /// <summary>Tear the native watcher down. Called when the last subscriber goes away.</summary>
    protected abstract void StopListening();


    /// <summary>
    /// Re-reads <see cref="CurrentNetwork"/> and raises <see cref="Changed"/> only if it differs
    /// from the last state seen. Call this from the native watcher.
    /// </summary>
    protected void RaiseChangedIfDifferent()
    {
        WifiNetworkInfo? current;
        lock (this.stateLock)
        {
            current = this.CurrentNetwork;
            if (Equals(this.lastKnown, current))
                return;

            this.lastKnown = current;
        }
        this.changed?.Invoke(this, current);
    }


    public abstract Task<AccessState> RequestAccess(CancellationToken ct = default);


    public virtual Task<IReadOnlyList<WifiNetwork>> Scan(CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.Scan);

    public virtual Task<WifiNetworkInfo> Connect(WifiConnectionRequest request, CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.Connect);

    public virtual Task Disconnect(CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.Disconnect);

    public virtual Task<bool> GetRadioEnabled(CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.RadioState);

    public virtual Task SetRadioEnabled(bool enabled, CancellationToken ct = default)
        => throw this.NotSupported(WifiCapabilities.RadioToggle);


    /// <summary>
    /// The generic "this platform has no API for that" failure. Override the operation and throw a
    /// more specific message wherever there is something useful to tell the caller.
    /// </summary>
    protected WifiNotSupportedException NotSupported(WifiCapabilities capability)
        => WifiNotSupportedException.For(capability, "the operating system exposes no API for it to third-party apps");
}
