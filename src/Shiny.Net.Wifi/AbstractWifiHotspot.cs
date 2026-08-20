namespace Shiny.Net.Wifi;


/// <summary>
/// Shared plumbing for the platform <see cref="IWifiHotspot"/> implementations - tracks the running
/// session, raises <see cref="Changed"/> around start and stop, and guarantees only one hotspot is
/// up at a time.
/// </summary>
public abstract class AbstractWifiHotspot : IWifiHotspot
{
    IHotspotSession? session;

    public abstract bool IsSupported { get; }
    public event EventHandler<HotspotInfo?>? Changed;

    public HotspotInfo? Current
    {
        get
        {
            var current = this.session;
            return current?.IsRunning == true ? current.Info : null;
        }
    }


    public async Task<IHotspotSession> Start(HotspotConfiguration? config = null, CancellationToken ct = default)
    {
        // no platform here supports two access points at once, and silently returning the running
        // one would hand the caller a hotspot with settings they did not ask for
        await this.Stop(ct).ConfigureAwait(false);

        var started = await this.StartNative(config, ct).ConfigureAwait(false);
        this.session = started;
        this.Changed?.Invoke(this, started.Info);
        return started;
    }


    public async Task Stop(CancellationToken ct = default)
    {
        var running = Interlocked.Exchange(ref this.session, null);
        if (running == null)
            return;

        await running.Stop(ct).ConfigureAwait(false);
        this.Changed?.Invoke(this, null);
    }


    /// <summary>Raise the access point. The base class has already stopped any previous one.</summary>
    protected abstract Task<IHotspotSession> StartNative(HotspotConfiguration? config, CancellationToken ct);


    /// <summary>Called by a session that stopped on its own - the OS tore it down, or it was disposed directly.</summary>
    protected void OnSessionEnded(IHotspotSession ended)
    {
        if (Interlocked.CompareExchange(ref this.session, null, ended) == ended)
            this.Changed?.Invoke(this, null);
    }
}
