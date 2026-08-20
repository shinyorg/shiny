namespace Shiny.Net.Wifi;


/// <summary>
/// Hotspot stub for plain .NET. See <see cref="NetWifiManager"/> for why, and for the Linux route.
/// </summary>
public class NetWifiHotspot : AbstractWifiHotspot
{
    public override bool IsSupported => false;

    protected override Task<IHotspotSession> StartNative(HotspotConfiguration? config, CancellationToken ct)
        => throw WifiNotSupportedException.For(
            WifiCapabilities.Hotspot,
            "the base Shiny.Net.Wifi package has no native backend for plain .NET. On Linux, reference Shiny.Net.Wifi.Linux for the NetworkManager implementation"
        );
}
