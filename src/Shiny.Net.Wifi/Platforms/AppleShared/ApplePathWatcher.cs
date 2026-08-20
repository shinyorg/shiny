using CoreFoundation;
using Network;

namespace Shiny.Net.Wifi;


/// <summary>
/// Watches the Wi-Fi interface with NWPathMonitor and calls back whenever the path moves.
/// </summary>
/// <remarks>
/// Network.framework is the only change notification Apple offers that covers iOS, Mac Catalyst and
/// macOS identically. It reports reachability rather than Wi-Fi state, so it fires on route and DNS
/// changes that have nothing to do with the SSID - the manager de-duplicates those before raising
/// anything to the caller.
/// </remarks>
class ApplePathWatcher : IDisposable
{
    readonly Action onChanged;
    readonly DispatchQueue queue = new("shiny.net.wifi.path");
    NWPathMonitor? monitor;

    public ApplePathWatcher(Action onChanged) => this.onChanged = onChanged;


    public void Start()
    {
        if (this.monitor != null)
            return;

        this.monitor = new NWPathMonitor(NWInterfaceType.Wifi);
        this.monitor.SetQueue(this.queue);
        this.monitor.SnapshotHandler = _ => this.onChanged();
        this.monitor.Start();
    }


    public void Stop()
    {
        this.monitor?.Cancel();
        this.monitor?.Dispose();
        this.monitor = null;
    }


    public void Dispose() => this.Stop();
}
