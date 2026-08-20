using CoreWlan;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Net.Wifi.Internals;

namespace Shiny.Net.Wifi;


/// <summary>
/// macOS Wi-Fi through CoreWLAN, which unlike iOS exposes the whole station side of the radio -
/// scanning, associating, disassociating and powering the interface.
/// </summary>
/// <remarks>
/// <para>Since Sonoma, scan results and the joined SSID come back empty or as a placeholder unless
/// the app has been granted location access, so <see cref="RequestAccess"/> asks for it. A sandboxed
/// app also needs <c>com.apple.security.network.client</c>.</para>
/// <para>CoreWLAN's calls are synchronous and a scan blocks for several seconds, so the blocking
/// ones are pushed off the calling thread here.</para>
/// </remarks>
public class MacOSWifiManager(ILogger<MacOSWifiManager> logger) : AbstractWifiManager, IDisposable
{
    readonly AppleLocationAccess location = new();
    ApplePathWatcher? watcher;

    static CWInterface? Interface => CWWiFiClient.SharedWiFiClient.MainInterface;


    public override WifiCapabilities Capabilities =>
        WifiCapabilities.Scan |
        WifiCapabilities.Connect |
        WifiCapabilities.Disconnect |
        WifiCapabilities.CurrentNetwork |
        WifiCapabilities.RadioState |
        WifiCapabilities.RadioToggle;


    public override WifiNetworkInfo? CurrentNetwork
    {
        get
        {
            var iface = Interface;
            if (iface?.Ssid == null)
                return null;

            var addressing = ManagedNetworkInfo.Read(iface.InterfaceName)
                ?? new WifiNetworkInfo { InterfaceName = iface.InterfaceName ?? "en0" };

            var rssi = (int)iface.RssiValue;

            return addressing with
            {
                Ssid = iface.Ssid,
                Bssid = iface.Bssid,
                Security = ToSecurity(iface.Security),
                SignalStrengthDbm = rssi,
                SignalStrengthPercent = WifiChannels.ToPercent(rssi),
                FrequencyMhz = ToFrequency(iface.WlanChannel)
            };
        }
    }


    protected override void StartListening()
    {
        this.watcher = new ApplePathWatcher(this.RaiseChangedIfDifferent);
        this.watcher.Start();
        logger.WatcherStarted(nameof(ApplePathWatcher));
    }


    protected override void StopListening()
    {
        this.watcher?.Dispose();
        this.watcher = null;
    }


    public override Task<AccessState> RequestAccess(CancellationToken ct = default)
        => this.location.Request(ct);


    public override async Task<IReadOnlyList<WifiNetwork>> Scan(CancellationToken ct = default)
    {
        var iface = RequireInterface();

        var results = await Task
            .Run(() =>
            {
                // CoreWLAN takes nil here to mean "every network" rather than one by name; the
                // binding types the parameter as non-nullable, hence the suppression. The call
                // blocks for the duration of the scan, which is why it is not on the caller's thread
                var found = iface.ScanForNetworksWithName(null!, out var error);
                if (error != null)
                    throw new WifiException($"CoreWLAN could not scan - {error.LocalizedDescription}");

                return found ?? Array.Empty<CWNetwork>();
            }, ct)
            .ConfigureAwait(false);

        var networks = results
            .Select(ToNetwork)
            .OrderByDescending(x => x.SignalStrengthPercent)
            .ToArray();

        logger.ScanCompleted(networks.Length);
        return networks;
    }


    public override async Task<WifiNetworkInfo> Connect(WifiConnectionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var iface = RequireInterface();
        logger.Connecting(request.Ssid);

        await Task
            .Run(() =>
            {
                // AssociateToNetwork needs a scanned CWNetwork - there is no associate-by-name -
                // so a targeted scan runs first, which also covers hidden networks
                var candidates = iface.ScanForNetworksWithName(request.Ssid, out var scanError);
                if (scanError != null)
                    throw new WifiConnectionException($"Could not scan for '{request.Ssid}' - {scanError.LocalizedDescription}");

                var target = (candidates ?? Array.Empty<CWNetwork>())
                    .Where(x => request.Bssid == null || String.Equals(x.Bssid, request.Bssid, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => (int)x.RssiValue)
                    .FirstOrDefault();

                if (target == null)
                    throw new WifiConnectionException($"Network '{request.Ssid}' was not found in range");

                if (!iface.AssociateToNetwork(target, request.Passphrase, out var joinError))
                    throw new WifiConnectionException($"Could not join '{request.Ssid}' - {joinError?.LocalizedDescription ?? "the association was refused"}");
            }, ct)
            .ConfigureAwait(false);

        return await this
            .WaitForAddress(request.Ssid, request.Timeout ?? TimeSpan.FromSeconds(30), ct)
            .ConfigureAwait(false);
    }


    public override Task Disconnect(CancellationToken ct = default)
    {
        RequireInterface().Disassociate();
        return Task.CompletedTask;
    }


    public override Task<bool> GetRadioEnabled(CancellationToken ct = default)
        => Task.FromResult(RequireInterface().PowerOn);


    public override Task SetRadioEnabled(bool enabled, CancellationToken ct = default)
    {
        if (!RequireInterface().SetPower(enabled, out var error))
            throw new WifiException($"Could not switch the Wi-Fi radio - {error?.LocalizedDescription ?? "the request was refused"}");

        logger.RadioToggled(enabled);
        return Task.CompletedTask;
    }


    /// <remarks>
    /// Association completes before DHCP does, so handing back the network the moment CoreWLAN
    /// returns would give the caller an entry with no address on it.
    /// </remarks>
    async Task<WifiNetworkInfo> WaitForAddress(string ssid, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        while (!cts.IsCancellationRequested)
        {
            var current = this.CurrentNetwork;
            if (current?.IpAddresses.Count > 0)
                return current;

            try
            {
                await Task.Delay(500, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                break;
            }
        }
        ct.ThrowIfCancellationRequested();
        throw new WifiConnectionException($"Joined '{ssid}' but no address was assigned within {timeout}");
    }


    static CWInterface RequireInterface()
        => Interface ?? throw new WifiException("This Mac has no Wi-Fi interface");


    static WifiNetwork ToNetwork(CWNetwork native)
    {
        var rssi = (int)native.RssiValue;
        return new WifiNetwork
        {
            Ssid = native.Ssid ?? String.Empty,
            Bssid = native.Bssid,
            Security = ToSecurity(native),
            SignalStrengthDbm = rssi,
            SignalStrengthPercent = WifiChannels.ToPercent(rssi),
            FrequencyMhz = ToFrequency(native.WlanChannel),
            IsHidden = String.IsNullOrEmpty(native.Ssid)
        };
    }


    static int? ToFrequency(CWChannel? channel)
    {
        if (channel == null)
            return null;

        var band = channel.ChannelBand switch
        {
            CWChannelBand.TwoGHz => WifiBand.TwoPointFourGhz,
            CWChannelBand.FiveGHz => WifiBand.FiveGhz,
            CWChannelBand.SixGHz => WifiBand.SixGhz,
            _ => WifiBand.Unknown
        };
        return WifiChannels.ToFrequency(band, (int)channel.ChannelNumber);
    }


    /// <remarks>
    /// A CWNetwork does not name its scheme, it answers yes/no per scheme, and one in a transition
    /// mode says yes to several. Probed strongest first so a WPA2/WPA3 transition network is not
    /// reported as the weaker of the two.
    /// </remarks>
    static WifiSecurity ToSecurity(CWNetwork network)
    {
        if (network.SupportsSecurity(CWSecurity.Wpa3Personal) || network.SupportsSecurity(CWSecurity.Wpa3Transition))
            return WifiSecurity.Wpa3Psk;

        if (network.SupportsSecurity(CWSecurity.Wpa3Enterprise) ||
            network.SupportsSecurity(CWSecurity.WPA2Enterprise) ||
            network.SupportsSecurity(CWSecurity.WPAEnterprise))
            return WifiSecurity.Enterprise;

        if (network.SupportsSecurity(CWSecurity.Owe) || network.SupportsSecurity(CWSecurity.OweTransition))
            return WifiSecurity.Owe;

        if (network.SupportsSecurity(CWSecurity.WPA2Personal))
            return WifiSecurity.Wpa2Psk;

        if (network.SupportsSecurity(CWSecurity.WPAPersonal))
            return WifiSecurity.WpaPsk;

        if (network.SupportsSecurity(CWSecurity.WEP))
            return WifiSecurity.Wep;

        return network.SupportsSecurity(CWSecurity.None) ? WifiSecurity.Open : WifiSecurity.Unknown;
    }


    static WifiSecurity ToSecurity(CWSecurity security) => security switch
    {
        CWSecurity.None => WifiSecurity.Open,
        CWSecurity.WEP => WifiSecurity.Wep,
        CWSecurity.DynamicWEP => WifiSecurity.Wep,
        CWSecurity.WPAPersonal => WifiSecurity.WpaPsk,
        CWSecurity.WPAPersonalMixed => WifiSecurity.WpaPsk,
        CWSecurity.WPA2Personal => WifiSecurity.Wpa2Psk,
        CWSecurity.Personal => WifiSecurity.Wpa2Psk,
        CWSecurity.Wpa3Personal => WifiSecurity.Wpa3Psk,
        CWSecurity.Wpa3Transition => WifiSecurity.Wpa3Psk,
        CWSecurity.Owe => WifiSecurity.Owe,
        CWSecurity.OweTransition => WifiSecurity.Owe,
        CWSecurity.WPAEnterprise => WifiSecurity.Enterprise,
        CWSecurity.WPAEnterpriseMixed => WifiSecurity.Enterprise,
        CWSecurity.WPA2Enterprise => WifiSecurity.Enterprise,
        CWSecurity.Wpa3Enterprise => WifiSecurity.Enterprise,
        CWSecurity.Enterprise => WifiSecurity.Enterprise,
        _ => WifiSecurity.Unknown
    };


    public void Dispose()
    {
        this.StopListening();
        this.location.Dispose();
    }
}
