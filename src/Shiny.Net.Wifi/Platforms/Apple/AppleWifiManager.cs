using Foundation;
using Microsoft.Extensions.Logging;
using NetworkExtension;
using Shiny.Net.Wifi.Internals;
using SystemConfiguration;

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
/// <para>Reading the SSID goes through CaptiveNetwork, which needs the <c>Access WiFi Information</c>
/// capability and, since iOS 13, granted location access. Missing either returns a placeholder name
/// rather than failing, so call <see cref="RequestAccess"/> first.</para>
/// <para>Scanning is absent by design: the only API that lists nearby networks lives inside a
/// NEHotspotHelper, and that entitlement is granted case by case by Apple.</para>
/// <para>The configurations your app has applied can be listed and removed - that is what
/// <see cref="GetKnownNetworks"/> and <see cref="Forget"/> map onto - but they cannot be re-joined
/// on demand. A stored configuration is a standing instruction iOS acts on when the network is in
/// range, so <see cref="Connect(string, CancellationToken)"/> throws here.</para>
/// </remarks>
public class AppleWifiManager(ILogger<AppleWifiManager> logger) : AbstractWifiManager, IDisposable
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


    public override WifiNetworkInfo? CurrentNetwork
    {
        get
        {
            if (CaptiveNetwork.TryGetSupportedInterfaces(out var interfaces) != StatusCode.OK || interfaces == null)
                return null;

            foreach (var name in interfaces.OfType<string>().Where(x => x.Length > 0))
            {
                var info = ReadInterface(name);
                if (info != null)
                    return info;
            }
            return null;
        }
    }


    static WifiNetworkInfo? ReadInterface(string name)
    {
        if (CaptiveNetwork.TryCopyCurrentNetworkInfo(name, out var dictionary) != StatusCode.OK || dictionary == null)
            return null;

        using (dictionary)
        {
            var addressing = ManagedNetworkInfo.Read(name) ?? new WifiNetworkInfo { InterfaceName = name };

            return addressing with
            {
                Ssid = (dictionary[CaptiveNetwork.NetworkInfoKeySSID] as NSString)?.ToString(),
                Bssid = (dictionary[CaptiveNetwork.NetworkInfoKeyBSSID] as NSString)?.ToString()
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

    public override async Task<WifiNetworkInfo?> GetCurrentNetwork(CancellationToken ct = default)
    {
        if (OperatingSystem.IsIOSVersionAtLeast(14) || OperatingSystem.IsMacCatalyst())
        {
            var hotspotNetwork = await NEHotspotNetwork.FetchCurrentAsync().WaitAsync(ct);

            var info = ManagedNetworkInfo.Read() ?? new WifiNetworkInfo { InterfaceName = "WiFi" };
            return info with
            {
                Ssid = hotspotNetwork.Ssid,
                Bssid = hotspotNetwork.Bssid,
                SignalStrengthPercent = (int)(hotspotNetwork.SignalStrength * 10),
                Security = info.Security != WifiSecurity.Unknown ? info.Security : hotspotNetwork.SecurityType switch
                {
                    NEHotspotNetworkSecurityType.Open => WifiSecurity.Open,
                    NEHotspotNetworkSecurityType.Wep => WifiSecurity.Wep,
                    NEHotspotNetworkSecurityType.Personal => WifiSecurity.WpaPsk, //Not true, so perhaps unkown would be more 'correct'?
                    NEHotspotNetworkSecurityType.Enterprise => WifiSecurity.Enterprise,
                    _ => WifiSecurity.Unknown,
                }
            };
        }

        return this.CurrentNetwork;
    }


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

        return this.CurrentNetwork
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
    public override Task Disconnect(CancellationToken ct = default)
    {
        var ssid = this.CurrentNetwork?.Ssid;
        if (ssid != null)
            NEHotspotConfigurationManager.SharedManager.RemoveConfiguration(ssid);

        return Task.CompletedTask;
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
