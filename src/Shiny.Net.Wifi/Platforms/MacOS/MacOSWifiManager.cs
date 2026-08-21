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
/// <para>Saved networks are the machine's preferred-network list, readable by anyone but editable
/// only with an administrator authorization - see <see cref="Forget"/>.</para>
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
        WifiCapabilities.RadioToggle |
        WifiCapabilities.KnownNetworks |
        WifiCapabilities.ForgetNetwork |
        WifiCapabilities.ConnectKnownNetwork;


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


    /// <remarks>
    /// The passphrase comes out of the login keychain, so a profile saved by the user joins without
    /// your app ever seeing it - but the network still has to be in range, because CoreWLAN
    /// associates to a scanned access point rather than to a name.
    /// </remarks>
    public override async Task<WifiNetworkInfo> Connect(string knownNetworkId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knownNetworkId);
        var iface = RequireInterface();
        logger.Connecting(knownNetworkId);

        await Task
            .Run(() =>
            {
                var candidates = iface.ScanForNetworksWithName(knownNetworkId, out var scanError);
                if (scanError != null)
                    throw new WifiConnectionException($"Could not scan for '{knownNetworkId}' - {scanError.LocalizedDescription}");

                var target = (candidates ?? Array.Empty<CWNetwork>())
                    .OrderByDescending(x => (int)x.RssiValue)
                    .FirstOrDefault();

                if (target == null)
                    throw new WifiConnectionException($"Saved network '{knownNetworkId}' was not found in range");

                // a null passphrase tells CoreWLAN to use the credential already in the keychain,
                // which is the whole point of joining by saved profile
                if (!iface.AssociateToNetwork(target, null!, out var joinError))
                    throw new WifiConnectionException($"Could not rejoin '{knownNetworkId}' - {joinError?.LocalizedDescription ?? "the association was refused"}");
            }, ct)
            .ConfigureAwait(false);

        return await this
            .WaitForAddress(knownNetworkId, TimeSpan.FromSeconds(30), ct)
            .ConfigureAwait(false);
    }


    public override Task Disconnect(CancellationToken ct = default)
    {
        RequireInterface().Disassociate();
        return Task.CompletedTask;
    }


    /// <remarks>
    /// The whole machine's preferred-network list, in the order macOS tries them - not just the
    /// profiles this app added, which CoreWLAN does not distinguish.
    /// </remarks>
    public override Task<IReadOnlyList<KnownWifiNetwork>> GetKnownNetworks(CancellationToken ct = default)
    {
        var profiles = RequireInterface().Configuration?.NetworkProfiles ?? Array.Empty<CWNetworkProfile>();

        var results = profiles
            .Where(x => !String.IsNullOrEmpty(x.Ssid))
            .Select(x => new KnownWifiNetwork
            {
                // CoreWLAN gives a profile no identity beyond its SSID
                Id = x.Ssid!,
                Ssid = x.Ssid!,
                Security = ToSecurity(x.Security)
            })
            .ToList();

        logger.KnownNetworksRead(results.Count);
        return Task.FromResult<IReadOnlyList<KnownWifiNetwork>>(results);
    }


    /// <remarks>
    /// Editing the preferred-network list means committing a whole new CWConfiguration, and macOS
    /// gates that behind an SFAuthorization that a plain app cannot raise. Expect this to throw
    /// <see cref="WifiPermissionException"/> unless the process is already running with the rights -
    /// there is no third-party route around it, which is why the capability is advertised but the
    /// call may still fail.
    /// </remarks>
    public override Task Forget(string knownNetworkId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knownNetworkId);
        var iface = RequireInterface();

        var current = iface.Configuration?.NetworkProfiles ?? Array.Empty<CWNetworkProfile>();
        var remaining = current
            .Where(x => !String.Equals(x.Ssid, knownNetworkId, StringComparison.Ordinal))
            .ToArray();

        if (remaining.Length == current.Length)
            return Task.CompletedTask;

        logger.Forgetting(knownNetworkId);

        // a mutable copy carries the rest of the configuration across - building a fresh
        // CWMutableConfiguration would reset RememberJoinedNetworks and the administrator flags
        var config = (CWMutableConfiguration)iface.Configuration!.MutableCopy(null!);
        config.NetworkProfiles = new NSOrderedSet<CWNetworkProfile>(remaining);

        // the second argument is an SFAuthorization; there is no binding for one and no way for a
        // sandboxed app to obtain it, so this succeeds only where the process already holds the right
        if (!iface.CommitConfiguration(config, null!, out var error))
        {
            throw new WifiPermissionException(
                $"macOS refused to remove the saved network '{knownNetworkId}' - {error?.LocalizedDescription ?? "committing the Wi-Fi configuration needs an administrator authorization"}"
            );
        }
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
