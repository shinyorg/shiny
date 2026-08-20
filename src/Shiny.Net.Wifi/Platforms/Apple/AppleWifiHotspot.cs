namespace Shiny.Net.Wifi;


/// <summary>
/// There is no hotspot API on iOS or Mac Catalyst. Personal Hotspot is a system feature with no
/// app-facing surface - not even a way to read whether it is running.
/// </summary>
public class AppleWifiHotspot : AbstractWifiHotspot
{
    public override bool IsSupported => false;

    protected override Task<IHotspotSession> StartNative(HotspotConfiguration? config, CancellationToken ct)
        => throw WifiNotSupportedException.For(
            WifiCapabilities.Hotspot,
            "Apple exposes no hotspot API to apps. Personal Hotspot can only be started by the user from Settings"
        );
}
