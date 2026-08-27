using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using Shiny.Net.Wifi.Internals;
using Windows.Devices.Radios;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;

namespace Shiny.Net.Wifi;


/// <summary>
/// Windows Wi-Fi through the WiFiAdapter WinRT API.
/// </summary>
/// <remarks>
/// <para>Needs the <c>wiFiControl</c> capability in the app manifest for scanning and joining, and
/// <c>radios</c> to power the adapter. A packaged app without them gets
/// <see cref="WifiAccessStatus.DeniedBySystem"/> back from the consent prompt, which surfaces here
/// as <see cref="WifiPermissionException"/> rather than an empty scan.</para>
/// <para>The current SSID comes from the connection profile rather than the adapter, because the
/// adapter only knows what it last scanned - a network joined before the app started would
/// otherwise read as null.</para>
/// <para>Saved profiles are outside WinRT entirely - WiFiAdapter cannot list, delete or join one -
/// so those three go through <c>wlanapi.dll</c> directly. See <see cref="WlanApi"/>.</para>
/// </remarks>
public class WindowsWifiManager(ILogger<WindowsWifiManager> logger) : AbstractWifiManager(logger)
{
    static readonly TimeSpan defaultConnectTimeout = TimeSpan.FromSeconds(30);

    WiFiAdapter? adapter;
    NetworkStatusChangedEventHandler? statusHandler;


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


    /// <remarks>
    /// WinRT answers synchronously here, so this only wraps the read to satisfy the interface.
    /// </remarks>
    public override Task<WifiNetworkInfo?> GetCurrentNetwork(CancellationToken ct = default)
        => Task.FromResult(this.Read());


    WifiNetworkInfo? Read()
    {
        var profile = NetworkInformation
            .GetConnectionProfiles()
            .FirstOrDefault(x => x.IsWlanConnectionProfile && x.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.None);

        if (profile == null)
            return null;

        var ssid = profile.WlanConnectionProfileDetails?.GetConnectedSsid();
        var info = ManagedNetworkInfo.Read(FindInterfaceName(profile));
        if (info == null)
            return null;

        // NetworkReport is whatever the last scan produced, so signal is best-effort - it is
        // null until something has scanned, and stale afterwards. Never worth failing over.
        var seen = this.adapter?
            .NetworkReport?
            .AvailableNetworks
            .Where(x => x.Ssid == ssid)
            .OrderByDescending(x => x.NetworkRssiInDecibelMilliwatts)
            .FirstOrDefault();

        var rssi = seen == null ? (int?)null : (int)seen.NetworkRssiInDecibelMilliwatts;

        return info with
        {
            Ssid = ssid,
            Bssid = seen?.Bssid,
            Security = ToSecurity(seen?.SecuritySettings),
            SignalStrengthDbm = rssi,
            SignalStrengthPercent = rssi == null ? null : WifiChannels.ToPercent(rssi.Value),
            FrequencyMhz = seen == null ? null : seen.ChannelCenterFrequencyInKilohertz / 1000
        };
    }


    protected override void StartListening()
    {
        this.statusHandler = _ => this.RaiseChangedIfDifferent();
        NetworkInformation.NetworkStatusChanged += this.statusHandler;
        logger.WatcherStarted(nameof(NetworkInformation.NetworkStatusChanged));
    }


    protected override void StopListening()
    {
        if (this.statusHandler != null)
        {
            NetworkInformation.NetworkStatusChanged -= this.statusHandler;
            this.statusHandler = null;
        }
    }


    public override async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        var status = await WiFiAdapter.RequestAccessAsync().AsTask(ct).ConfigureAwait(false);
        return status switch
        {
            WiFiAccessStatus.Allowed => AccessState.Available,
            WiFiAccessStatus.DeniedByUser => AccessState.Denied,
            WiFiAccessStatus.DeniedBySystem => AccessState.NotSetup,
            _ => AccessState.Unknown
        };
    }


    public override async Task<IReadOnlyList<WifiNetwork>> Scan(CancellationToken ct = default)
    {
        var wifi = await this.GetAdapter(ct).ConfigureAwait(false);
        await wifi.ScanAsync().AsTask(ct).ConfigureAwait(false);

        var results = wifi
            .NetworkReport
            .AvailableNetworks
            .Select(ToNetwork)
            .OrderByDescending(x => x.SignalStrengthPercent)
            .ToArray();

        logger.ScanCompleted(results.Length);
        return results;
    }


    public override async Task<WifiNetworkInfo> Connect(WifiConnectionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var wifi = await this.GetAdapter(ct).ConfigureAwait(false);

        // ConnectAsync only takes a WiFiAvailableNetwork, never a bare name, so the network has to
        // be in the current report - a stale one is why "it worked a minute ago" fails here
        await wifi.ScanAsync().AsTask(ct).ConfigureAwait(false);

        var target = wifi
            .NetworkReport
            .AvailableNetworks
            .Where(x => x.Ssid == request.Ssid)
            .Where(x => request.Bssid == null || String.Equals(x.Bssid, request.Bssid, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.NetworkRssiInDecibelMilliwatts)
            .FirstOrDefault();

        if (target == null)
            throw new WifiConnectionException($"Network '{request.Ssid}' was not found in range");

        logger.Connecting(request.Ssid);

        var reconnect = request.Remember ? WiFiReconnectionKind.Automatic : WiFiReconnectionKind.Manual;
        var credential = request.Passphrase == null ? null : new PasswordCredential { Password = request.Passphrase };

        var result = credential == null
            ? await wifi.ConnectAsync(target, reconnect).AsTask(ct).ConfigureAwait(false)
            : await wifi.ConnectAsync(target, reconnect, credential).AsTask(ct).ConfigureAwait(false);

        if (result.ConnectionStatus != WiFiConnectionStatus.Success)
            throw new WifiConnectionException($"Could not join '{request.Ssid}' - {result.ConnectionStatus}");

        return await this
            .WaitForAddress(request.Ssid, request.Timeout ?? defaultConnectTimeout, ct)
            .ConfigureAwait(false);
    }


    /// <remarks>
    /// Joins by profile, so Windows supplies the credential it already holds and the network does
    /// not have to be in the current scan report the way
    /// <see cref="Connect(WifiConnectionRequest, CancellationToken)"/> requires.
    /// </remarks>
    public override async Task<WifiNetworkInfo> Connect(string knownNetworkId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knownNetworkId);

        var profile = await Task
            .Run(() => WlanApi.GetProfiles().FirstOrDefault(x => x.Name == knownNetworkId), ct)
            .ConfigureAwait(false)
            ?? throw new WifiConnectionException($"No saved profile named '{knownNetworkId}'");

        logger.Connecting(knownNetworkId);
        await Task.Run(() => WlanApi.Connect(profile.InterfaceId, profile.Name), ct).ConfigureAwait(false);

        return await this
            .WaitForAddress(knownNetworkId, defaultConnectTimeout, ct)
            .ConfigureAwait(false);
    }


    public override async Task Disconnect(CancellationToken ct = default)
    {
        var wifi = await this.GetAdapter(ct).ConfigureAwait(false);
        wifi.Disconnect();
    }


    /// <remarks>
    /// Every profile on the machine, whoever saved it - Windows keeps one store per adapter and
    /// does not record which app wrote an entry, so <see cref="KnownWifiNetwork.AddedByThisApp"/>
    /// is always false even for profiles this app created.
    /// </remarks>
    public override async Task<IReadOnlyList<KnownWifiNetwork>> GetKnownNetworks(CancellationToken ct = default)
    {
        // the WLAN calls are blocking and read one profile's XML at a time, so a machine with a
        // long history spends real time here
        var profiles = await Task.Run(WlanApi.GetProfiles, ct).ConfigureAwait(false);

        var results = profiles
            .Select(x => new KnownWifiNetwork
            {
                // profile names are unique per adapter and, for anything Windows saved itself,
                // identical to the SSID
                Id = x.Name,
                Ssid = x.Name,
                Security = x.Security,
                IsHidden = x.IsHidden
            })
            .ToList();

        logger.KnownNetworksRead(results.Count);
        return results;
    }


    public override async Task Forget(string knownNetworkId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knownNetworkId);
        logger.Forgetting(knownNetworkId);

        // a false return means no adapter had a profile by that name, which is the state the
        // caller asked for
        await Task.Run(() => WlanApi.DeleteProfile(knownNetworkId), ct).ConfigureAwait(false);
    }


    public override async Task<bool> GetRadioEnabled(CancellationToken ct = default)
    {
        var radio = await GetWifiRadio(ct).ConfigureAwait(false);
        return radio?.State == RadioState.On;
    }


    public override async Task SetRadioEnabled(bool enabled, CancellationToken ct = default)
    {
        var radio = await GetWifiRadio(ct).ConfigureAwait(false);
        if (radio == null)
            throw new WifiException("No Wi-Fi radio was found on this machine");

        var access = await radio
            .SetStateAsync(enabled ? RadioState.On : RadioState.Off)
            .AsTask(ct)
            .ConfigureAwait(false);

        if (access != RadioAccessStatus.Allowed)
            throw new WifiPermissionException($"The Wi-Fi radio could not be switched - {access}. Declare the 'radios' capability in the app manifest");

        logger.RadioToggled(enabled);
    }


    internal static async Task<Radio?> GetWifiRadio(CancellationToken ct)
    {
        var access = await Radio.RequestAccessAsync().AsTask(ct).ConfigureAwait(false);
        if (access != RadioAccessStatus.Allowed)
            throw new WifiPermissionException($"Access to the machine's radios was refused - {access}. Declare the 'radios' capability in the app manifest");

        var radios = await Radio.GetRadiosAsync().AsTask(ct).ConfigureAwait(false);
        return radios.FirstOrDefault(x => x.Kind == RadioKind.WiFi);
    }


    async Task<WiFiAdapter> GetAdapter(CancellationToken ct)
    {
        if (this.adapter != null)
            return this.adapter;

        var access = await this.RequestAccess(ct).ConfigureAwait(false);
        if (access != AccessState.Available)
            throw new WifiPermissionException($"Access to the Wi-Fi adapter was refused ({access}). Declare the 'wiFiControl' capability in the app manifest");

        var adapters = await WiFiAdapter.FindAllAdaptersAsync().AsTask(ct).ConfigureAwait(false);
        this.adapter = adapters.FirstOrDefault() ?? throw new WifiException("No Wi-Fi adapter was found on this machine");
        return this.adapter;
    }


    /// <remarks>
    /// WinRT identifies an adapter by GUID while System.Net.NetworkInformation identifies it by a
    /// braced string form of the same GUID, so the two views are joined on that.
    /// </remarks>
    static string? FindInterfaceName(ConnectionProfile profile)
    {
        var id = profile.NetworkAdapter?.NetworkAdapterId;
        if (id == null)
            return null;

        var text = id.Value.ToString();
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .FirstOrDefault(x => x.Id.Trim('{', '}').Equals(text, StringComparison.OrdinalIgnoreCase))?
            .Name;
    }


    static WifiNetwork ToNetwork(WiFiAvailableNetwork native)
    {
        var rssi = (int)native.NetworkRssiInDecibelMilliwatts;
        return new WifiNetwork
        {
            Ssid = native.Ssid,
            Bssid = native.Bssid,
            Security = ToSecurity(native.SecuritySettings),
            SignalStrengthDbm = rssi,
            SignalStrengthPercent = WifiChannels.ToPercent(rssi),
            FrequencyMhz = native.ChannelCenterFrequencyInKilohertz / 1000,
            IsHidden = String.IsNullOrEmpty(native.Ssid)
        };
    }


    static WifiSecurity ToSecurity(NetworkSecuritySettings? settings) => settings?.NetworkAuthenticationType switch
    {
        null => WifiSecurity.Unknown,
        NetworkAuthenticationType.Open80211 => WifiSecurity.Open,
        NetworkAuthenticationType.None => WifiSecurity.Open,
        NetworkAuthenticationType.SharedKey80211 => WifiSecurity.Wep,
        NetworkAuthenticationType.WpaPsk => WifiSecurity.WpaPsk,
        NetworkAuthenticationType.WpaNone => WifiSecurity.WpaPsk,
        NetworkAuthenticationType.RsnaPsk => WifiSecurity.Wpa2Psk,
        NetworkAuthenticationType.Wpa3Sae => WifiSecurity.Wpa3Psk,
        NetworkAuthenticationType.Wpa3 => WifiSecurity.Wpa3Psk,
        NetworkAuthenticationType.Owe => WifiSecurity.Owe,
        // Wpa and Rsna are the 802.1X forms - the PSK variants have their own values above
        NetworkAuthenticationType.Wpa => WifiSecurity.Enterprise,
        NetworkAuthenticationType.Rsna => WifiSecurity.Enterprise,
        _ => WifiSecurity.Unknown
    };
}
