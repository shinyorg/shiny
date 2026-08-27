using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using Shiny.Net.Wifi.Internals;

namespace Shiny.Net.Wifi;


/// <summary>
/// The fallback for plain .NET, where there is no OS Wi-Fi API to call.
/// </summary>
/// <remarks>
/// <para>Addressing still works - <see cref="GetCurrentNetwork"/> reports the IP, DNS, gateway and
/// mask of the wireless interface, and <see cref="Changed"/> fires off NetworkChange - because that
/// all comes from the managed network stack. Everything Wi-Fi specific (SSID, signal, scanning,
/// joining) needs native code and throws.</para>
/// <para>On Linux, reference <c>Shiny.Net.Wifi.Linux</c> instead: it talks to NetworkManager over
/// D-Bus and supports the whole API. This type is what a Windows or macOS console app that
/// referenced the base package gets, and it is deliberately a stub rather than a lie.</para>
/// </remarks>
public class NetWifiManager(ILogger<NetWifiManager> logger) : AbstractWifiManager(logger)
{
    NetworkAddressChangedEventHandler? addressHandler;
    NetworkAvailabilityChangedEventHandler? availabilityHandler;

    public override WifiCapabilities Capabilities => WifiCapabilities.None;

    public override Task<WifiNetworkInfo?> GetCurrentNetwork(CancellationToken ct = default)
        => Task.FromResult(ManagedNetworkInfo.Read());


    protected override void StartListening()
    {
        this.addressHandler = (_, _) => this.RaiseChangedIfDifferent();
        this.availabilityHandler = (_, _) => this.RaiseChangedIfDifferent();
        NetworkChange.NetworkAddressChanged += this.addressHandler;
        NetworkChange.NetworkAvailabilityChanged += this.availabilityHandler;
        logger.WatcherStarted(nameof(NetworkChange));
    }


    protected override void StopListening()
    {
        if (this.addressHandler != null)
        {
            NetworkChange.NetworkAddressChanged -= this.addressHandler;
            this.addressHandler = null;
        }
        if (this.availabilityHandler != null)
        {
            NetworkChange.NetworkAvailabilityChanged -= this.availabilityHandler;
            this.availabilityHandler = null;
        }
    }


    // addressing works, the Wi-Fi operations do not - which is exactly what Restricted describes
    public override Task<AccessState> RequestAccess(CancellationToken ct = default)
        => Task.FromResult(AccessState.Restricted);


    public override Task<IReadOnlyList<WifiNetwork>> Scan(CancellationToken ct = default)
        => throw NoNativeApi(WifiCapabilities.Scan);

    public override Task<WifiNetworkInfo> Connect(WifiConnectionRequest request, CancellationToken ct = default)
        => throw NoNativeApi(WifiCapabilities.Connect);

    public override Task Disconnect(CancellationToken ct = default)
        => throw NoNativeApi(WifiCapabilities.Disconnect);

    public override Task<bool> GetRadioEnabled(CancellationToken ct = default)
        => throw NoNativeApi(WifiCapabilities.RadioState);

    public override Task SetRadioEnabled(bool enabled, CancellationToken ct = default)
        => throw NoNativeApi(WifiCapabilities.RadioToggle);


    static WifiNotSupportedException NoNativeApi(WifiCapabilities capability)
        => WifiNotSupportedException.For(
            capability,
            "the base Shiny.Net.Wifi package has no native backend for plain .NET. On Linux, reference Shiny.Net.Wifi.Linux for the NetworkManager implementation"
        );
}
