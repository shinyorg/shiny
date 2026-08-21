using System.Net;
using System.Runtime.Versioning;
using Android;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Microsoft.Extensions.Logging;
using Shiny.Net.Wifi.Internals;
using Debug = System.Diagnostics.Debug;

namespace Shiny.Net.Wifi;


/// <summary>
/// Android Wi-Fi across WifiManager (scanning, radio state) and ConnectivityManager (joining,
/// addressing), which is the split Android has enforced since API 29.
/// </summary>
/// <remarks>
/// <para>Manifest needs <c>ACCESS_WIFI_STATE</c>, <c>CHANGE_WIFI_STATE</c> and
/// <c>ACCESS_FINE_LOCATION</c>; from API 33 add <c>NEARBY_WIFI_DEVICES</c>. Scanning and the SSID
/// of the joined network both return empty rather than failing when location has not been granted,
/// so call <see cref="RequestAccess"/> first.</para>
/// <para>From API 29 joining goes through a WifiNetworkSpecifier: Android shows the user a dialog
/// naming the network, the join is never saved, and it lasts only while your app holds the request -
/// which is why the callback is kept alive here until <see cref="Disconnect"/>. Older releases get
/// the legacy WifiConfiguration path, which does persist.</para>
/// </remarks>
public class AndroidWifiManager(
    AndroidPlatform platform,
    ILogger<AndroidWifiManager> logger
) : AbstractWifiManager, IDisposable
{
    static readonly TimeSpan defaultConnectTimeout = TimeSpan.FromSeconds(30);
    static readonly TimeSpan scanTimeout = TimeSpan.FromSeconds(20);

    ConnectivityManager.NetworkCallback? changeCallback;
    ConnectivityManager.NetworkCallback? requestCallback;
    Network? boundNetwork;

    Android.Net.Wifi.WifiManager Native => platform.GetSystemService<Android.Net.Wifi.WifiManager>(Context.WifiService);
    ConnectivityManager Connectivity => platform.GetSystemService<ConnectivityManager>(Context.ConnectivityService);


    public override WifiCapabilities Capabilities
    {
        get
        {
            var caps = WifiCapabilities.Scan |
                       WifiCapabilities.Connect |
                       WifiCapabilities.Disconnect |
                       WifiCapabilities.CurrentNetwork |
                       WifiCapabilities.RadioState;

            // SetWifiEnabled became a no-op returning false for third-party apps in API 29, and the
            // legacy configuration list it goes with is the only saved network an app can join on
            // demand - a suggestion is a hint the OS acts on when it likes, not a call
            if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                caps |= WifiCapabilities.RadioToggle |
                        WifiCapabilities.ConnectKnownNetwork |
                        WifiCapabilities.KnownNetworks |
                        WifiCapabilities.ForgetNetwork;
            }
            else if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                // API 29 is the gap: it accepts suggestions but cannot enumerate them, so there is
                // no way to list what is saved or to name one to remove
                caps |= WifiCapabilities.KnownNetworks | WifiCapabilities.ForgetNetwork;
            }

            return caps;
        }
    }


    public override WifiNetworkInfo? CurrentNetwork
    {
        get
        {
            var connectivity = this.Connectivity;

            // a specifier-bound network is the one this app asked for, and is not necessarily the
            // system's active network - prefer it when we are holding one
            var network = this.boundNetwork ?? connectivity.ActiveNetwork;
            if (network == null)
                return null;

            var caps = connectivity.GetNetworkCapabilities(network);
            if (caps?.HasTransport(TransportType.Wifi) != true)
                return null;

            var link = connectivity.GetLinkProperties(network);
            if (link == null)
                return null;

            var wifiInfo = GetWifiInfo(caps, this.Native);
            var rssi = wifiInfo?.Rssi;

            return new WifiNetworkInfo
            {
                InterfaceName = link.InterfaceName ?? "wlan0",
                Ssid = Unquote(wifiInfo?.SSID),
                Bssid = Normalise(wifiInfo?.BSSID),
                Security = ToSecurity(wifiInfo),
                IpAddresses = link.LinkAddresses
                    .Select(x => Convert(x.Address))
                    .Where(x => x != null)
                    .ToArray()!,
                DnsAddresses = link.DnsServers
                    .Select(Convert)
                    .Where(x => x != null)
                    .ToArray()!,
                Gateway = link.Routes
                    .Where(x => x.IsDefaultRoute)
                    .Select(x => Convert(x.Gateway))
                    .FirstOrDefault(x => x != null),
                SubnetMask = ToMask(link),
                SignalStrengthDbm = rssi,
                SignalStrengthPercent = rssi == null ? null : WifiChannels.ToPercent(rssi.Value),
                FrequencyMhz = wifiInfo?.Frequency
            };
        }
    }


    /// <remarks>
    /// WifiManager.ConnectionInfo was deprecated in API 31 in favour of pulling the WifiInfo off the
    /// network's capabilities, which is also the only way to see a specifier-bound network.
    /// </remarks>
    static WifiInfo? GetWifiInfo(NetworkCapabilities caps, Android.Net.Wifi.WifiManager native)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
            return caps.TransportInfo as WifiInfo;

        return LegacyConnectionInfo(native);
    }


#pragma warning disable CA1422 // ConnectionInfo is deprecated in API 31+, but we only reach it below 31 where NetworkCapabilities.TransportInfo does not carry WifiInfo
    static WifiInfo? LegacyConnectionInfo(Android.Net.Wifi.WifiManager native) => native.ConnectionInfo;
#pragma warning restore CA1422


    protected override void StartListening()
    {
        var request = new NetworkRequest.Builder()
            .AddTransportType(TransportType.Wifi)!
            .Build()!;

        this.changeCallback = new WifiChangeCallback(this.RaiseChangedIfDifferent);
        this.Connectivity.RegisterNetworkCallback(request, this.changeCallback);
        logger.WatcherStarted(nameof(ConnectivityManager.NetworkCallback));
    }


    protected override void StopListening()
    {
        if (this.changeCallback != null)
        {
            this.Connectivity.UnregisterNetworkCallback(this.changeCallback);
            this.changeCallback = null;
        }
    }


    public override async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        // NEARBY_WIFI_DEVICES was introduced in API 33 so an app can scan without asking for
        // location, but location is still what unlocks the SSID of the joined network
        var permissions = OperatingSystem.IsAndroidVersionAtLeast(33)
            ? new[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.NearbyWifiDevices }
            : new[] { Manifest.Permission.AccessFineLocation };

        var result = await platform.RequestPermissions(ct, permissions).ConfigureAwait(false);
        if (result.IsSuccess())
            return AccessState.Available;

        return result.IsGranted(Manifest.Permission.AccessFineLocation)
            ? AccessState.Restricted
            : AccessState.Denied;
    }


    public override async Task<IReadOnlyList<WifiNetwork>> Scan(CancellationToken ct = default)
    {
        var native = this.Native;
        if (!native.IsWifiEnabled)
            throw new WifiException("The Wi-Fi radio is off, so there is nothing to scan");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(scanTimeout);

        var tcs = new TaskCompletionSource();
        await using var subscription = this.SubscribeToScanResults(native, () => tcs.TrySetResult()).ConfigureAwait(false);

        // API 28 and below let an app scan as often as it liked; every release since throttles it,
        // and a throttled StartScan returns false having quietly served the cached results instead.
        // It is marked deprecated from 28 with no replacement for third-party apps - the successor
        // (WifiScanner) is a system API - so this stays the only way to ask for a fresh scan.
#pragma warning disable CA1422
        var accepted = native.StartScan();
#pragma warning restore CA1422
        if (!accepted)
            logger.ScanThrottled();

        using (cts.Token.Register(() => tcs.TrySetResult()))
            await tcs.Task.ConfigureAwait(false);

        ct.ThrowIfCancellationRequested();

        var results = native
            .ScanResults?
            .Select(ToNetwork)
            .OrderByDescending(x => x.SignalStrengthPercent)
            .ToArray() ?? Array.Empty<WifiNetwork>();

        if (results.Length == 0)
            throw new WifiPermissionException("The scan returned nothing. Android reports an empty list rather than an error when ACCESS_FINE_LOCATION (or NEARBY_WIFI_DEVICES on API 33+) has not been granted - call RequestAccess() first");

        logger.ScanCompleted(results.Length);
        return results;
    }


    /// <remarks>
    /// ScanResultsCallback arrived in API 30. Below that the only signal is the broadcast, which is
    /// implicit and so cannot be received at all from a manifest-declared receiver on API 26+ -
    /// it has to be registered from code, as it is here.
    /// </remarks>
    IAsyncDisposable SubscribeToScanResults(Android.Net.Wifi.WifiManager native, Action onResults)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            return this.SubscribeViaCallback(native, onResults);

        var receiver = new ScanReceiver(onResults);
        var filter = new IntentFilter(Android.Net.Wifi.WifiManager.ScanResultsAvailableAction);
        platform.AppContext.RegisterReceiver(receiver, filter);
        return new AsyncDisposable(() => platform.AppContext.UnregisterReceiver(receiver));
    }


    [SupportedOSPlatform("android30.0")]
    IAsyncDisposable SubscribeViaCallback(Android.Net.Wifi.WifiManager native, Action onResults)
    {
        var callback = new ScanCallback(onResults);
        native.RegisterScanResultsCallback(platform.AppContext.MainExecutor!, callback);
        return new AsyncDisposable(() => native.UnregisterScanResultsCallback(callback));
    }


    public override async Task<WifiNetworkInfo> Connect(WifiConnectionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        logger.Connecting(request.Ssid);

        await this.Disconnect(ct).ConfigureAwait(false);

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            // a specifier join is what actually gets the device onto the network now, but it is
            // never saved - so when the caller asked to be remembered, a suggestion goes in
            // alongside it purely to persist the credential for later
            if (request.Remember && OperatingSystem.IsAndroidVersionAtLeast(30))
                this.Suggest(request);

            await this.ConnectViaSpecifier(request, ct).ConfigureAwait(false);
        }
        else
        {
            this.ConnectViaConfiguration(request);
        }

        return await this
            .WaitForAddress(request.Ssid, request.Timeout ?? defaultConnectTimeout, ct)
            .ConfigureAwait(false);
    }


    /// <remarks>
    /// Suggestions are additive and duplicates are rejected, so an SSID already suggested is
    /// removed first - which also lets a changed passphrase replace the stored one. A failure here
    /// is never fatal: the specifier join that follows is what the caller is waiting on, and losing
    /// the ability to auto-rejoin later is not worth failing that.
    /// </remarks>
    [SupportedOSPlatform("android30.0")]
    void Suggest(WifiConnectionRequest request)
    {
        try
        {
            var native = this.Native;
            var existing = native
                .NetworkSuggestions?
                .Where(x => x.Ssid == request.Ssid)
                .ToList();

            if (existing?.Count > 0)
                native.RemoveNetworkSuggestions(existing);

            var builder = new WifiNetworkSuggestion.Builder()
                .SetSsid(request.Ssid)!
                .SetIsHiddenSsid(request.IsHidden)!;

            if (request.Bssid != null)
                builder = builder.SetBssid(MacAddress.FromString(request.Bssid))!;

            if (request.Passphrase != null)
            {
                builder = request.Security == WifiSecurity.Wpa3Psk
                    ? builder.SetWpa3Passphrase(request.Passphrase)!
                    : builder.SetWpa2Passphrase(request.Passphrase)!;
            }
            else if (request.Security == WifiSecurity.Owe)
            {
                builder = builder.SetIsEnhancedOpen(true)!;
            }

            var status = native.AddNetworkSuggestions([builder.Build()!]);
            if (status == NetworkStatus.SuggestionsSuccess)
                logger.Suggested(request.Ssid);
            else
                logger.WifiError(new WifiException($"Android refused the suggestion - {status}"), $"Could not save '{request.Ssid}' for later");
        }
        catch (Exception ex)
        {
            logger.WifiError(ex, $"Could not save '{request.Ssid}' for later");
        }
    }


    [SupportedOSPlatform("android29.0")]
    async Task ConnectViaSpecifier(WifiConnectionRequest request, CancellationToken ct)
    {
        var builder = new WifiNetworkSpecifier.Builder()
            .SetSsid(request.Ssid)!
            .SetIsHiddenSsid(request.IsHidden)!;

        if (request.Bssid != null)
            builder = builder.SetBssid(MacAddress.FromString(request.Bssid))!;

        if (request.Passphrase != null)
        {
            builder = request.Security == WifiSecurity.Wpa3Psk
                ? builder.SetWpa3Passphrase(request.Passphrase)!
                : builder.SetWpa2Passphrase(request.Passphrase)!;
        }
        else if (request.Security == WifiSecurity.Owe)
        {
            builder = builder.SetIsEnhancedOpen(true)!;
        }

        var networkRequest = new NetworkRequest.Builder()
            .AddTransportType(TransportType.Wifi)!
            // the specifier network is app-scoped and carries no internet capability of its own,
            // so leaving the default NET_CAPABILITY_INTERNET on the request never matches
            .RemoveCapability(NetCapability.Internet)!
            .SetNetworkSpecifier(builder.Build()!)!
            .Build()!;

        var tcs = new TaskCompletionSource<Network>();
        var callback = new WifiRequestCallback(
            network => tcs.TrySetResult(network),
            () => tcs.TrySetException(new WifiConnectionException($"Android could not join '{request.Ssid}' - the network was unavailable or the user declined the prompt"))
        );

        this.Connectivity.RequestNetwork(networkRequest, callback);
        this.requestCallback = callback;

        try
        {
            using (ct.Register(() => tcs.TrySetCanceled(ct)))
                this.boundNetwork = await tcs.Task.ConfigureAwait(false);
        }
        catch
        {
            await this.Disconnect(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }


    /// <remarks>
    /// The pre-29 path. AddNetwork/EnableNetwork were turned into no-ops for third-party apps in
    /// API 29, which is why this is gated rather than used as a fallback.
    /// </remarks>
#pragma warning disable CA1422 // deliberately the legacy path - only reached below API 29
    void ConnectViaConfiguration(WifiConnectionRequest request)
    {
        var native = this.Native;
        var config = new WifiConfiguration
        {
            // the legacy API wants the SSID quoted and the passphrase quoted with it
            Ssid = $"\"{request.Ssid}\"",
            HiddenSSID = request.IsHidden
        };

        if (request.Passphrase == null)
            config.AllowedKeyManagement!.Set((int)KeyManagementType.None);
        else
            config.PreSharedKey = $"\"{request.Passphrase}\"";

        if (request.Bssid != null)
            config.Bssid = request.Bssid;

        var networkId = native.AddNetwork(config);
        if (networkId == -1)
            throw new WifiConnectionException($"Android refused the configuration for '{request.Ssid}'");

        if (!native.EnableNetwork(networkId, true))
            throw new WifiConnectionException($"Android would not enable the network '{request.Ssid}'");
    }


    /// <remarks>
    /// Only reachable below API 29, where a saved network is a WifiConfiguration with a numeric id
    /// that <c>enableNetwork</c> accepts. From API 29 the OS decides when to act on a suggestion
    /// and there is no equivalent call, so this throws.
    /// </remarks>
    public override async Task<WifiNetworkInfo> Connect(string knownNetworkId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knownNetworkId);

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            throw WifiNotSupportedException.For(
                WifiCapabilities.ConnectKnownNetwork,
                "Android 10 removed enableNetwork for third-party apps. A saved network here is a WifiNetworkSuggestion, which the OS joins on its own schedule - call Connect(WifiConnectionRequest) with the passphrase to join now"
            );
        }

        var known = (await this.GetKnownNetworks(ct).ConfigureAwait(false))
            .FirstOrDefault(x => x.Id == knownNetworkId)
            ?? throw new WifiConnectionException($"No saved network with id '{knownNetworkId}'");

        this.EnableSavedNetwork(knownNetworkId, known.Ssid);

        return await this
            .WaitForAddress(known.Ssid, defaultConnectTimeout, ct)
            .ConfigureAwait(false);
    }


#pragma warning disable CA1422 // deliberately the legacy path - only reached below API 29
    void EnableSavedNetwork(string id, string ssid)
    {
        if (!Int32.TryParse(id, out var networkId))
            throw new WifiConnectionException($"'{id}' is not an Android network id");

        if (!this.Native.EnableNetwork(networkId, true))
            throw new WifiConnectionException($"Android would not enable the saved network '{ssid}'");
    }
#pragma warning restore CA1422


    public override Task Disconnect(CancellationToken ct = default)
    {
        if (this.requestCallback != null)
        {
            // releasing the request is what drops a specifier network - there is no disconnect call
            this.Connectivity.UnregisterNetworkCallback(this.requestCallback);
            this.requestCallback = null;
            this.boundNetwork = null;
        }
        else if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            this.Native.Disconnect();
        }
        return Task.CompletedTask;
    }


    /// <remarks>
    /// <para>Android never discloses the user's own saved networks to an app. What comes back is
    /// this app's entries only: the suggestions it added (API 30+, which is where
    /// <see cref="WifiConnectionRequest.Remember"/> puts them) plus any configurations it created
    /// itself (API 31+, and in practice only on a device that was upgraded from Android 9).</para>
    /// <para>Below API 29 the legacy configuration list is readable in full, and each entry carries
    /// the numeric network id that <see cref="Connect(string, CancellationToken)"/> takes.</para>
    /// </remarks>
    public override Task<IReadOnlyList<KnownWifiNetwork>> GetKnownNetworks(CancellationToken ct = default)
    {
        List<KnownWifiNetwork> results;
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            results = this.ReadLegacyConfigurations();

        else if (OperatingSystem.IsAndroidVersionAtLeast(30))
            results = this.ReadSuggestions();

        else
        {
            throw WifiNotSupportedException.For(
                WifiCapabilities.KnownNetworks,
                "Android 10 accepts network suggestions but cannot enumerate them - getNetworkSuggestions only arrived in Android 11, and the legacy configuration list was closed off in Android 10"
            );
        }

        logger.KnownNetworksRead(results.Count);
        return Task.FromResult<IReadOnlyList<KnownWifiNetwork>>(results);
    }


    [SupportedOSPlatform("android30.0")]
    List<KnownWifiNetwork> ReadSuggestions()
    {
        var native = this.Native;
        var results = (native.NetworkSuggestions ?? [])
            .Where(x => !String.IsNullOrEmpty(x.Ssid))
            .Select(x => new KnownWifiNetwork
            {
                // a suggestion has no handle of its own - it is matched back by SSID
                Id = x.Ssid!,
                Ssid = x.Ssid!,
                Security = ToSecurity(x),
                IsHidden = x.IsHiddenSsid,
                AddedByThisApp = true
            })
            .ToList();

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            // an SSID can be both suggested and configured; the suggestion is the richer entry, so
            // it wins and the configuration is folded in only where there is no suggestion for it
            var seen = results.Select(x => x.Ssid).ToHashSet(StringComparer.Ordinal);

            var configured = (native.CallerConfiguredNetworks ?? [])
                .Select(x => Unquote(x.Ssid))
                .Where(x => !String.IsNullOrEmpty(x) && seen.Add(x))
                .Select(x => new KnownWifiNetwork
                {
                    Id = x!,
                    Ssid = x!,
                    AddedByThisApp = true
                })
                .ToList();

            results.AddRange(configured);
        }
        return results;
    }


#pragma warning disable CA1422 // deliberately the legacy path - only reached below API 29
    List<KnownWifiNetwork> ReadLegacyConfigurations()
        => (this.Native.ConfiguredNetworks ?? [])
            .Where(x => !String.IsNullOrEmpty(Unquote(x.Ssid)))
            .Select(x => new KnownWifiNetwork
            {
                // pre-29 Android issues a real handle, so use it rather than the name
                Id = x.NetworkId.ToString(),
                Ssid = Unquote(x.Ssid)!,
                IsHidden = x.HiddenSSID
            })
            .ToList();
#pragma warning restore CA1422


    /// <remarks>
    /// Removes the suggestion your app added (API 30+) or the legacy configuration it created.
    /// Networks the user saved themselves are neither visible nor removable from here.
    /// </remarks>
    public override Task Forget(string knownNetworkId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knownNetworkId);
        logger.Forgetting(knownNetworkId);

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            this.ForgetSuggestion(knownNetworkId);
        else if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            this.ForgetLegacyConfiguration(knownNetworkId);
        else
        {
            throw WifiNotSupportedException.For(
                WifiCapabilities.ForgetNetwork,
                "Android 10 exposes no way to read back or remove a saved network - the suggestion API only became enumerable in Android 11"
            );
        }
        return Task.CompletedTask;
    }


    [SupportedOSPlatform("android30.0")]
    void ForgetSuggestion(string ssid)
    {
        var native = this.Native;
        var matches = (native.NetworkSuggestions ?? [])
            .Where(x => x.Ssid == ssid)
            .ToList();

        // already gone is a success, not a failure - Forget is meant to be safe to call blind
        if (matches.Count == 0)
            return;

        var status = native.RemoveNetworkSuggestions(matches);
        if (status != NetworkStatus.SuggestionsSuccess)
            throw new WifiException($"Android would not remove the saved network '{ssid}' - {status}");
    }


#pragma warning disable CA1422 // deliberately the legacy path - only reached below API 29
    void ForgetLegacyConfiguration(string id)
    {
        if (!Int32.TryParse(id, out var networkId))
            throw new WifiException($"'{id}' is not an Android network id");

        var native = this.Native;
        if (native.RemoveNetwork(networkId))
            native.SaveConfiguration();
    }
#pragma warning restore CA1422


    public override Task<bool> GetRadioEnabled(CancellationToken ct = default)
        => Task.FromResult(this.Native.IsWifiEnabled);


    public override Task SetRadioEnabled(bool enabled, CancellationToken ct = default)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            throw WifiNotSupportedException.For(
                WifiCapabilities.RadioToggle,
                "Android 10 (API 29) revoked WifiManager.setWifiEnabled for third-party apps. Send the user to Settings.Panel.ACTION_WIFI instead"
            );
        }

        if (!this.Native.SetWifiEnabled(enabled))
            throw new WifiException("Android refused to switch the Wi-Fi radio");

        logger.RadioToggled(enabled);
        return Task.CompletedTask;
    }
#pragma warning restore CA1422


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


    static WifiNetwork ToNetwork(ScanResult native)
    {
        var ssid = Unquote(ReadSsid(native)) ?? String.Empty;
        return new WifiNetwork
        {
            Ssid = ssid,
            Bssid = Normalise(native.Bssid),
            Security = WifiSecurityParser.Parse(native.Capabilities),
            SignalStrengthDbm = native.Level,
            SignalStrengthPercent = WifiChannels.ToPercent(native.Level),
            FrequencyMhz = native.Frequency,
            IsHidden = ssid.Length == 0
        };
    }


    /// <remarks>
    /// ScanResult.Ssid was obsoleted in API 33 in favour of the WifiSsid object, which carries the
    /// raw bytes rather than a string - the SSID is not required to be UTF-8. We support API 26, so
    /// the string form is still the only one available across the whole range.
    /// </remarks>
#pragma warning disable CA1422
    static string? ReadSsid(ScanResult native) => native.Ssid;
#pragma warning restore CA1422


    /// <remarks>
    /// WifiInfo.CurrentSecurityType arrived in API 31. Below that there is nothing on WifiInfo that
    /// names the scheme - the capability string only exists on a ScanResult - so it stays Unknown.
    /// </remarks>
    static WifiSecurity ToSecurity(WifiInfo? info)
    {
        if (info == null || !OperatingSystem.IsAndroidVersionAtLeast(31))
            return WifiSecurity.Unknown;

        return (WifiSecurityType)info.CurrentSecurityType switch
        {
            WifiSecurityType.Open => WifiSecurity.Open,
            WifiSecurityType.Wep => WifiSecurity.Wep,
            WifiSecurityType.Psk => WifiSecurity.Wpa2Psk,
            WifiSecurityType.Sae => WifiSecurity.Wpa3Psk,
            WifiSecurityType.Owe => WifiSecurity.Owe,
            WifiSecurityType.Eap => WifiSecurity.Enterprise,
            WifiSecurityType.EapWpa3Enterprise => WifiSecurity.Enterprise,
            WifiSecurityType.EapWpa3Enterprise192Bit => WifiSecurity.Enterprise,
            _ => WifiSecurity.Unknown
        };
    }


    /// <remarks>
    /// A suggestion does not report its scheme, only which passphrase slot was filled - which is
    /// enough to tell WPA3 from WPA2 from open, and is all we ever put in one.
    /// </remarks>
    [SupportedOSPlatform("android29.0")]
    static WifiSecurity ToSecurity(WifiNetworkSuggestion suggestion)
    {
        if (suggestion.EnterpriseConfig != null)
            return WifiSecurity.Enterprise;

        if (suggestion.IsEnhancedOpen)
            return WifiSecurity.Owe;

        return suggestion.Passphrase == null ? WifiSecurity.Open : WifiSecurity.Unknown;
    }


    // Android hands the SSID back wrapped in quotes, and "<unknown ssid>" when location was refused
    static string? Unquote(string? ssid)
    {
        if (String.IsNullOrEmpty(ssid) || ssid == "<unknown ssid>")
            return null;

        return ssid.Length >= 2 && ssid[0] == '"' && ssid[^1] == '"'
            ? ssid[1..^1]
            : ssid;
    }


    // the all-zero BSSID is what a WifiInfo carries when the caller lacks location permission
    static string? Normalise(string? bssid)
        => String.IsNullOrEmpty(bssid) || bssid == "02:00:00:00:00:00" ? null : bssid;


    static IPAddress? Convert(Java.Net.InetAddress? address)
    {
        var bytes = address?.GetAddress();
        return bytes == null ? null : new IPAddress(bytes);
    }


    /// <remarks>
    /// LinkAddress carries a prefix length rather than a mask, which is the more useful form
    /// everywhere except the one place callers expect a dotted mask.
    /// </remarks>
    static IPAddress? ToMask(LinkProperties link)
    {
        var v4 = link.LinkAddresses.FirstOrDefault(x => x.Address is Java.Net.Inet4Address);
        if (v4 == null)
            return null;

        var prefix = v4.PrefixLength;
        Debug.Assert(prefix is >= 0 and <= 32);

        var mask = prefix == 0 ? 0u : UInt32.MaxValue << (32 - prefix);
        return new IPAddress(new[]
        {
            (byte)(mask >> 24),
            (byte)(mask >> 16),
            (byte)(mask >> 8),
            (byte)mask
        });
    }


    public void Dispose()
    {
        this.StopListening();
        this.Disconnect().GetAwaiter().GetResult();
    }
}


class WifiChangeCallback(Action onChange) : ConnectivityManager.NetworkCallback
{
    public override void OnAvailable(Network network) => onChange();
    public override void OnLost(Network network) => onChange();
    public override void OnUnavailable() => onChange();
    public override void OnLinkPropertiesChanged(Network network, LinkProperties linkProperties) => onChange();
    public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities) => onChange();
}


class WifiRequestCallback(Action<Network> onAvailable, Action onFailed) : ConnectivityManager.NetworkCallback
{
    public override void OnAvailable(Network network) => onAvailable(network);
    public override void OnUnavailable() => onFailed();
}


class ScanCallback(Action onResults) : Android.Net.Wifi.WifiManager.ScanResultsCallback
{
    public override void OnScanResultsAvailable() => onResults();
}


[BroadcastReceiver(Enabled = true, Exported = false)]
class ScanReceiver(Action onResults) : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent) => onResults();
}


class AsyncDisposable(Action dispose) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        dispose();
        return ValueTask.CompletedTask;
    }
}
