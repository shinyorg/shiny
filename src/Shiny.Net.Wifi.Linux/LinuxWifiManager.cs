using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Shiny.Net.Wifi.Internals;
using Shiny.Net.Wifi.NetworkManager;
using Tmds.DBus.Protocol;

namespace Shiny.Net.Wifi;


/// <summary>
/// Linux Wi-Fi through NetworkManager over the D-Bus system bus - the fullest implementation of
/// this API, since NetworkManager exposes everything an app might want and gates none of it behind
/// a store review.
/// </summary>
/// <remarks>
/// <para>Requires a running NetworkManager (the default on Ubuntu, Fedora, Debian desktop and
/// Raspberry Pi OS; not on a systemd-networkd or netplan-only server). Scanning and reading state
/// need no privileges. Connecting, disconnecting and toggling the radio go through polkit, which on
/// a desktop prompts the user and in a headless session needs a rule granting
/// <c>org.freedesktop.NetworkManager.network-control</c>.</para>
/// <para>D-Bus is asynchronous and <see cref="CurrentNetwork"/> is not, so the network state is
/// cached and refreshed from the PropertiesChanged watcher. Only the first read pays for a blocking
/// round trip.</para>
/// </remarks>
public class LinuxWifiManager(ILogger<LinuxWifiManager> logger) : AbstractWifiManager, IAsyncDisposable
{
    static readonly TimeSpan primeTimeout = TimeSpan.FromSeconds(5);

    readonly NmClient client = new();
    WifiNetworkInfo? cached;
    bool primed;
    IDisposable? watcher;
    string? activeConnectionPath;


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
            // Task.Run keeps this off any captured synchronization context - blocking on the
            // D-Bus round trip directly would deadlock a UI thread. Refresh sets `primed` itself,
            // so a read that times out is retried next time rather than latching null forever.
            if (!this.primed)
                Task.Run(() => this.Refresh(CancellationToken.None)).Wait(primeTimeout);

            return this.cached;
        }
    }


    async Task Refresh(CancellationToken ct)
    {
        try
        {
            this.cached = await this.Read(ct).ConfigureAwait(false);
            this.primed = true;
        }
        catch (Exception ex)
        {
            logger.WifiError(ex, "Could not read the current network from NetworkManager");
            this.cached = null;
        }
    }


    async Task<WifiNetworkInfo?> Read(CancellationToken ct)
    {
        var device = await this.client.GetWifiDevicePath(ct).ConfigureAwait(false);
        var state = await this.client.GetDeviceState(device, ct).ConfigureAwait(false);
        if (state != NmConstants.DeviceStateActivated)
            return null;

        var name = await this.client.GetInterfaceName(device, ct).ConfigureAwait(false);
        var ap = await this.client.GetActiveAccessPoint(device, ct).ConfigureAwait(false);
        var ip = await this.client.GetIp4Config(device, ct).ConfigureAwait(false);

        return new WifiNetworkInfo
        {
            InterfaceName = name,
            Ssid = ap?.Ssid,
            Bssid = ap?.Bssid,
            Security = ap?.Security ?? WifiSecurity.Unknown,
            IpAddresses = ip?.Addresses ?? Array.Empty<IPAddress>(),
            DnsAddresses = ip?.Nameservers ?? Array.Empty<IPAddress>(),
            Gateway = ip?.Gateway,
            SubnetMask = ip == null || ip.PrefixLength == 0 ? null : ToMask(ip.PrefixLength),
            // NetworkManager reports a 0-100 quality rather than an RSSI, and there is no lossless
            // way back to dBm - so the percentage is authoritative here and dBm stays null
            SignalStrengthPercent = ap?.Strength,
            FrequencyMhz = ap == null ? null : (int)ap.FrequencyMhz
        };
    }


    protected override void StartListening()
        => _ = this.StartWatching();


    async Task StartWatching()
    {
        try
        {
            this.watcher = await this.client
                .WatchPropertiesChanged(() =>
                {
                    _ = this.Refresh(CancellationToken.None)
                        .ContinueWith(_ => this.RaiseChangedIfDifferent(), TaskScheduler.Default);
                })
                .ConfigureAwait(false);

            logger.WatcherStarted("NetworkManager PropertiesChanged");
        }
        catch (Exception ex)
        {
            logger.WifiError(ex, "Could not subscribe to NetworkManager property changes");
        }
    }


    protected override void StopListening()
    {
        this.watcher?.Dispose();
        this.watcher = null;
    }


    /// <remarks>
    /// There is nothing to ask for. NetworkManager reads freely and gates the mutating calls behind
    /// polkit, which prompts at the point of use rather than up front.
    /// </remarks>
    public override async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        try
        {
            await this.client.GetWifiDevicePath(ct).ConfigureAwait(false);
            return await this.client.GetWirelessEnabled(ct).ConfigureAwait(false)
                ? AccessState.Available
                : AccessState.Disabled;
        }
        catch (WifiException)
        {
            return AccessState.NotSupported;
        }
        catch (DBusExceptionBase ex)
        {
            logger.WifiError(ex, "NetworkManager is not reachable on the system bus");
            return AccessState.NotSupported;
        }
    }


    public override async Task<IReadOnlyList<WifiNetwork>> Scan(CancellationToken ct = default)
    {
        var device = await this.client.GetWifiDevicePath(ct).ConfigureAwait(false);

        try
        {
            await this.client.RequestScan(device, ct).ConfigureAwait(false);

            // NetworkManager has no "scan finished" reply - the AccessPoints property fills in over
            // the following seconds, so this waits out a typical full-band sweep before reading
            await Task.Delay(TimeSpan.FromSeconds(4), ct).ConfigureAwait(false);
        }
        catch (DBusExceptionBase ex)
        {
            // a scan requested within ~10s of the last one is refused; the cached list is still good
            logger.WifiError(ex, "NetworkManager refused the scan request - reading cached access points");
        }

        var points = await this.client.GetAccessPoints(device, ct).ConfigureAwait(false);

        var results = points
            .Select(x => new WifiNetwork
            {
                Ssid = x.Ssid,
                Bssid = x.Bssid,
                Security = x.Security,
                SignalStrengthPercent = x.Strength,
                FrequencyMhz = (int)x.FrequencyMhz,
                IsHidden = x.Ssid.Length == 0
            })
            .OrderByDescending(x => x.SignalStrengthPercent)
            .ToArray();

        logger.ScanCompleted(results.Length);
        return results;
    }


    public override async Task<WifiNetworkInfo> Connect(WifiConnectionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        logger.Connecting(request.Ssid);

        var device = await this.client.GetWifiDevicePath(ct).ConfigureAwait(false);
        var settings = BuildStationSettings(request);

        // the "specific object" pins the activation to one access point; "/" lets NetworkManager
        // pick the strongest BSSID for the SSID, which is what you want unless a BSSID was named
        var specific = NmConstants.NullPath;
        if (request.Bssid != null)
        {
            var points = await this.client.GetAccessPoints(device, ct).ConfigureAwait(false);
            specific = points
                .FirstOrDefault(x => String.Equals(x.Bssid, request.Bssid, StringComparison.OrdinalIgnoreCase))?
                .Path ?? NmConstants.NullPath;
        }

        try
        {
            this.activeConnectionPath = await this.client
                .AddAndActivate(settings, device, specific, !request.Remember, ct)
                .ConfigureAwait(false);
        }
        catch (DBusExceptionBase ex)
        {
            throw new WifiConnectionException($"NetworkManager refused to join '{request.Ssid}' - {ex.Describe()}", ex);
        }

        return await this
            .WaitForAddress(request.Ssid, request.Timeout ?? TimeSpan.FromSeconds(30), ct)
            .ConfigureAwait(false);
    }


    static NmConnectionSettings BuildStationSettings(WifiConnectionRequest request)
    {
        var settings = new NmConnectionSettings();

        var connection = settings.Group("connection");
        connection["id"] = VariantValue.String(request.Ssid);
        connection["type"] = VariantValue.String("802-11-wireless");
        connection["autoconnect"] = VariantValue.Bool(request.Remember);

        var wireless = settings.Group("802-11-wireless");
        wireless["ssid"] = VariantValue.Array(Encoding.UTF8.GetBytes(request.Ssid));
        wireless["mode"] = VariantValue.String("infrastructure");
        wireless["hidden"] = VariantValue.Bool(request.IsHidden);

        if (request.Passphrase != null)
        {
            var security = settings.Group("802-11-wireless-security");
            security["key-mgmt"] = VariantValue.String(request.Security == WifiSecurity.Wpa3Psk ? "sae" : "wpa-psk");
            security["psk"] = VariantValue.String(request.Passphrase);
        }
        else if (request.Security == WifiSecurity.Owe)
        {
            var security = settings.Group("802-11-wireless-security");
            security["key-mgmt"] = VariantValue.String("owe");
        }

        return settings;
    }


    public override async Task Disconnect(CancellationToken ct = default)
    {
        var path = this.activeConnectionPath;
        if (path == null)
        {
            var device = await this.client.GetWifiDevicePath(ct).ConfigureAwait(false);
            path = await this.client.GetActiveConnection(device, ct).ConfigureAwait(false);
        }

        if (path == null || path == NmConstants.NullPath)
            return;

        await this.client.Deactivate(path, ct).ConfigureAwait(false);
        this.activeConnectionPath = null;
    }


    public override Task<bool> GetRadioEnabled(CancellationToken ct = default)
        => this.client.GetWirelessEnabled(ct);


    public override async Task SetRadioEnabled(bool enabled, CancellationToken ct = default)
    {
        try
        {
            await this.client.SetWirelessEnabled(enabled, ct).ConfigureAwait(false);
            logger.RadioToggled(enabled);
        }
        catch (DBusExceptionBase ex)
        {
            throw new WifiPermissionException($"NetworkManager refused to switch the Wi-Fi radio - {ex.Describe()}. This needs the polkit action org.freedesktop.NetworkManager.enable-disable-wifi", ex);
        }
    }


    async Task<WifiNetworkInfo> WaitForAddress(string ssid, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        while (!cts.IsCancellationRequested)
        {
            await this.Refresh(cts.Token).ConfigureAwait(false);
            if (this.cached?.IpAddresses.Count > 0)
                return this.cached;

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


    internal static IPAddress ToMask(int prefixLength)
    {
        var mask = prefixLength == 0 ? 0u : UInt32.MaxValue << (32 - prefixLength);
        return new IPAddress(new[]
        {
            (byte)(mask >> 24),
            (byte)(mask >> 16),
            (byte)(mask >> 8),
            (byte)mask
        });
    }


    public async ValueTask DisposeAsync()
    {
        this.StopListening();
        await this.client.DisposeAsync().ConfigureAwait(false);
    }
}
