using Foundation;
using Microsoft.Extensions.Logging;
using NetworkExtension;
using Shiny.Net.Wifi.Internals;

namespace Shiny.Net.Wifi;


/// <summary>
/// iOS and Mac Catalyst Wi-Fi, which is to say: joining a named network, leaving it, and reading
/// back which one you are on. There is no more surface than that.
/// </summary>
/// <remarks>
/// <para>Joining goes through NEHotspotConfiguration, which needs the <c>Hotspot Configuration</c>
/// capability on the App ID. The system shows its own confirmation dialog naming the network - your
/// app cannot join anything silently, and a user who declines surfaces here as
/// <see cref="WifiConnectionException"/>.</para>
/// <para>Reading the SSID goes through <c>NEHotspotNetwork.fetchCurrent</c>, which needs the
/// <c>Access WiFi Information</c> capability and granted location access. Missing either leaves the
/// SSID and BSSID null rather than failing, so call <see cref="RequestAccess"/> first. This is the
/// iOS 14 replacement for CaptiveNetwork's <c>CNCopyCurrentNetworkInfo</c>, which since iOS 14
/// returns nothing at all unless your own app configured the network it is being asked about.</para>
/// <para>Scanning is absent by design: the only API that lists nearby networks lives inside a
/// NEHotspotHelper, and that entitlement is granted case by case by Apple.</para>
/// <para>The configurations your app has applied can be listed and removed - that is what
/// <see cref="GetKnownNetworks"/> and <see cref="Forget"/> map onto - but they cannot be re-joined
/// on demand. A stored configuration is a standing instruction iOS acts on when the network is in
/// range, so <see cref="Connect(string, CancellationToken)"/> throws here.</para>
/// </remarks>
public class AppleWifiManager(ILogger<AppleWifiManager> logger) : AbstractWifiManager(logger), IDisposable
{
    // NEHotspotConfigurationError.AlreadyAssociated - the join succeeded before we asked
    const int AlreadyAssociated = 13;
    const int UserDenied = 7;

    readonly AppleLocationAccess location = new();
    ApplePathWatcher? watcher;


    public override WifiCapabilities Capabilities =>
        WifiCapabilities.Connect |
        WifiCapabilities.Disconnect |
        WifiCapabilities.CurrentNetwork |
        WifiCapabilities.KnownNetworks |
        WifiCapabilities.ForgetNetwork;


    /// <remarks>
    /// <para>Addressing comes from the managed stack and needs no permission; the SSID, BSSID,
    /// security and signal come from NEHotspotNetwork and need the <c>Access WiFi Information</c>
    /// capability plus location. Without those, iOS hands back nothing and the Wi-Fi half of the
    /// result stays null while the addressing half is still populated.</para>
    /// <para>NEHotspotNetwork also reports the security type and signal, which CaptiveNetwork never
    /// did - so both are filled in here rather than left at their defaults.</para>
    /// </remarks>
    public override async Task<WifiNetworkInfo?> GetCurrentNetwork(CancellationToken ct = default)
    {
        var addressing = ManagedNetworkInfo.Read();
        var network = await NEHotspotNetwork
            .FetchCurrentAsync()
            .WaitAsync(ct)
            .ConfigureAwait(false);

        if (network == null)
            return addressing;

        addressing ??= new WifiNetworkInfo { InterfaceName = "en0" };

        // iOS reports signal as 0-1 rather than an RSSI, and there is no lossless way back to dBm,
        // so the percentage is authoritative here and dBm stays null
        return addressing with
        {
            Ssid = network.Ssid,
            Bssid = network.Bssid,
            Security = ToSecurity(network),
            SignalStrengthPercent = (int)Math.Round(network.SignalStrength * 100)
        };
    }


    /// <remarks>
    /// iOS reports "personal" without naming the generation - it does not distinguish WPA2 from
    /// WPA3 - so that lands on the generation-agnostic <see cref="WifiSecurity.Psk"/> rather than
    /// guessing at one of the two.
    /// </remarks>
    static WifiSecurity ToSecurity(NEHotspotNetwork network)
        => network.SecurityType switch
        {
            NEHotspotNetworkSecurityType.Open => WifiSecurity.Open,
            NEHotspotNetworkSecurityType.Wep => WifiSecurity.Wep,
            NEHotspotNetworkSecurityType.Personal => WifiSecurity.Psk,
            NEHotspotNetworkSecurityType.Enterprise => WifiSecurity.Enterprise,
            _ => WifiSecurity.Unknown
        };


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


    public override Task<IReadOnlyList<WifiNetwork>> Scan(CancellationToken ct = default)
        => throw WifiNotSupportedException.For(
            WifiCapabilities.Scan,
            "iOS has no public scanning API. Listing nearby networks requires the NEHotspotHelper entitlement, which Apple grants case by case to captive network assistant apps"
        );


    public override async Task<WifiNetworkInfo> Connect(WifiConnectionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        logger.Connecting(request.Ssid);

        var config = Build(request);
        try
        {
            await NEHotspotConfigurationManager
                .SharedManager
                .ApplyConfigurationAsync(config)
                .WaitAsync(request.Timeout ?? TimeSpan.FromSeconds(30), ct)
                .ConfigureAwait(false);
        }
        catch (NSErrorException ex) when ((int)ex.Error.Code == AlreadyAssociated)
        {
            // iOS reports "you were already on this network" as an error; the caller asked to be
            // on it and is on it, so there is nothing to report
        }
        catch (NSErrorException ex) when ((int)ex.Error.Code == UserDenied)
        {
            throw new WifiConnectionException($"The user declined the system prompt to join '{request.Ssid}'", ex);
        }
        catch (NSErrorException ex)
        {
            throw new WifiConnectionException($"Could not join '{request.Ssid}' - {ex.Error.LocalizedDescription}", ex);
        }
        catch (TimeoutException ex)
        {
            throw new WifiConnectionException($"Timed out joining '{request.Ssid}'", ex);
        }

        return await this.GetCurrentNetwork(ct).ConfigureAwait(false)
            ?? throw new WifiConnectionException($"Applied the configuration for '{request.Ssid}' but no Wi-Fi interface came up");
    }


    static NEHotspotConfiguration Build(WifiConnectionRequest request)
    {
        var isWep = request.Security == WifiSecurity.Wep;

        var config = request.Passphrase == null
            ? new NEHotspotConfiguration(request.Ssid)
            : new NEHotspotConfiguration(request.Ssid, request.Passphrase, isWep);

        config.Hidden = request.IsHidden;

        // JoinOnce drops the configuration as soon as the app is suspended, which is the closest
        // iOS gets to "do not remember this network"
        config.JoinOnce = !request.Remember;
        return config;
    }


    /// <remarks>
    /// Removing the configuration is all iOS offers. It drops the network your app added; the OS is
    /// then free to rejoin any network the user had already saved, so the device does not
    /// necessarily end up off Wi-Fi.
    /// </remarks>
    public override async Task Disconnect(CancellationToken ct = default)
    {
        var ssid = (await this.GetCurrentNetwork(ct).ConfigureAwait(false))?.Ssid;
        if (ssid != null)
            NEHotspotConfigurationManager.SharedManager.RemoveConfiguration(ssid);
    }


    /// <remarks>
    /// <para>NEHotspotConfigurationManager only discloses the SSIDs <b>this app</b> configured, so
    /// the user's own saved networks never appear here - the list is empty until your app has
    /// called <see cref="Connect(WifiConnectionRequest, CancellationToken)"/> with
    /// <see cref="WifiConnectionRequest.Remember"/> left on.</para>
    /// <para>Only names come back. iOS reports no security type or hidden flag for a stored
    /// configuration, so those stay at their defaults.</para>
    /// </remarks>
    public override async Task<IReadOnlyList<KnownWifiNetwork>> GetKnownNetworks(CancellationToken ct = default)
    {
        var ssids = await NEHotspotConfigurationManager
            .SharedManager
            .GetConfiguredSsidsAsync()
            .WaitAsync(ct)
            .ConfigureAwait(false);

        var results = ssids
            .Select(x => new KnownWifiNetwork
            {
                // iOS keys its configurations by SSID and offers no other handle
                Id = x,
                Ssid = x,
                AddedByThisApp = true
            })
            .ToList();

        logger.KnownNetworksRead(results.Count);
        return results;
    }


    /// <remarks>
    /// Drops the configuration your app added. Any network the user saved themselves is untouched -
    /// and unreachable from here - so the device may simply rejoin it.
    /// </remarks>
    public override Task Forget(string knownNetworkId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knownNetworkId);
        logger.Forgetting(knownNetworkId);

        // no-ops when the SSID was never configured, which is the contract we want
        NEHotspotConfigurationManager.SharedManager.RemoveConfiguration(knownNetworkId);
        return Task.CompletedTask;
    }


    public override Task<bool> GetRadioEnabled(CancellationToken ct = default)
        => throw WifiNotSupportedException.For(WifiCapabilities.RadioState, "iOS does not disclose Wi-Fi radio state to apps");

    public override Task SetRadioEnabled(bool enabled, CancellationToken ct = default)
        => throw WifiNotSupportedException.For(WifiCapabilities.RadioToggle, "iOS does not let apps power the Wi-Fi radio");


    public void Dispose()
    {
        this.StopListening();
        this.location.Dispose();
    }
}
