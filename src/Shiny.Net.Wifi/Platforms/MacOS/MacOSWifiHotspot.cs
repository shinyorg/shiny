namespace Shiny.Net.Wifi;


/// <summary>
/// macOS has no hotspot API. Internet Sharing is a System Settings feature backed by private
/// daemons - CoreWLAN can put an interface into host-AP mode but offers no way to configure or
/// route it, so there is nothing here worth pretending to support.
/// </summary>
public class MacOSWifiHotspot : AbstractWifiHotspot
{
    public override bool IsSupported => false;

    protected override Task<IHotspotSession> StartNative(HotspotConfiguration? config, CancellationToken ct)
        => throw WifiNotSupportedException.For(
            WifiCapabilities.Hotspot,
            "macOS exposes no hotspot API. Internet Sharing can only be turned on by the user in System Settings"
        );
}
