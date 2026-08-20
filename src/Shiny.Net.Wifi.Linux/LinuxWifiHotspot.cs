using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Shiny.Net.Wifi.NetworkManager;
using Tmds.DBus.Protocol;

namespace Shiny.Net.Wifi;


/// <summary>
/// Linux access point hosting through NetworkManager's AP mode, with NAT and DHCP supplied by
/// NetworkManager's <c>shared</c> IPv4 method.
/// </summary>
/// <remarks>
/// <para>Unlike Android's local-only hotspot this is a full access point: the SSID, passphrase and
/// band are yours to choose, and clients are routed out through whatever other connection the
/// machine has. It needs a Wi-Fi adapter whose driver supports AP mode - most do, a few cheap USB
/// dongles do not, and those fail at activation rather than up front.</para>
/// <para>Bringing the access point up takes the Wi-Fi radio out of station mode, so the machine
/// drops off any network it was joined to unless the adapter supports concurrent station/AP.</para>
/// </remarks>
public class LinuxWifiHotspot(ILogger<LinuxWifiHotspot> logger) : AbstractWifiHotspot
{
    const string DefaultSsid = "shiny-hotspot";

    readonly NmClient client = new();

    public override bool IsSupported => true;


    protected override async Task<IHotspotSession> StartNative(HotspotConfiguration? config, CancellationToken ct)
    {
        var device = await this.client.GetWifiDevicePath(ct).ConfigureAwait(false);
        var interfaceName = await this.client.GetInterfaceName(device, ct).ConfigureAwait(false);

        var ssid = config?.Ssid ?? DefaultSsid;
        var passphrase = config?.Passphrase;
        var settings = BuildApSettings(ssid, passphrase, config?.Band ?? WifiBand.Unknown, config?.IsHidden ?? false);

        string activePath;
        try
        {
            activePath = await this.client
                .AddAndActivate(settings, device, NmConstants.NullPath, @volatile: true, ct)
                .ConfigureAwait(false);
        }
        catch (DBusExceptionBase ex)
        {
            throw new WifiException($"NetworkManager could not raise the access point - {ex.Describe()}. The adapter may not support AP mode, or the polkit action org.freedesktop.NetworkManager.network-control may be denied", ex);
        }

        var ip = await this.client.GetIp4Config(device, ct).ConfigureAwait(false);
        var info = new HotspotInfo
        {
            Ssid = ssid,
            Passphrase = passphrase,
            Security = passphrase == null ? WifiSecurity.Open : WifiSecurity.Wpa2Psk,
            Band = config?.Band ?? WifiBand.Unknown,
            Address = ip?.Addresses.FirstOrDefault()
        };

        logger.HotspotStarted(ssid);
        return new LinuxHotspotSession(this.client, activePath, interfaceName, info, this.OnSessionEnded, logger);
    }


    static NmConnectionSettings BuildApSettings(string ssid, string? passphrase, WifiBand band, bool hidden)
    {
        var settings = new NmConnectionSettings();

        var connection = settings.Group("connection");
        connection["id"] = VariantValue.String(ssid);
        connection["type"] = VariantValue.String("802-11-wireless");
        connection["autoconnect"] = VariantValue.Bool(false);

        var wireless = settings.Group("802-11-wireless");
        wireless["ssid"] = VariantValue.Array(Encoding.UTF8.GetBytes(ssid));
        wireless["mode"] = VariantValue.String("ap");
        wireless["hidden"] = VariantValue.Bool(hidden);

        // NetworkManager names the bands "bg" and "a" after the 802.11 amendments; it has no 6 GHz
        // AP band, so a 6 GHz request falls through to letting the driver choose
        var bandName = band switch
        {
            WifiBand.TwoPointFourGhz => "bg",
            WifiBand.FiveGhz => "a",
            _ => null
        };
        if (bandName != null)
            wireless["band"] = VariantValue.String(bandName);

        if (passphrase != null)
        {
            var security = settings.Group("802-11-wireless-security");
            security["key-mgmt"] = VariantValue.String("wpa-psk");
            security["psk"] = VariantValue.String(passphrase);
        }

        // "shared" is what turns this into a usable hotspot rather than an isolated AP - it starts
        // a DHCP server on the interface and NATs clients out through the machine's other route
        settings.Group("ipv4")["method"] = VariantValue.String("shared");
        settings.Group("ipv6")["method"] = VariantValue.String("ignore");

        return settings;
    }
}


class LinuxHotspotSession(
    NmClient client,
    string activeConnectionPath,
    string interfaceName,
    HotspotInfo info,
    Action<IHotspotSession> onEnded,
    ILogger logger
) : IHotspotSession
{
    bool stopped;

    public HotspotInfo Info => info;
    public bool IsRunning => !this.stopped;


    /// <remarks>
    /// NetworkManager does not expose the stations associated with an AP-mode device, so this reads
    /// the kernel's neighbour table instead and keeps the entries on the hotspot interface. A client
    /// appears once it has taken a DHCP lease and talked to the gateway, not the moment it
    /// associates, and stale entries linger for a minute or so after one leaves.
    /// </remarks>
    public Task<IReadOnlyList<HotspotClient>> GetClients(CancellationToken ct = default)
    {
        var clients = ArpTable
            .Read(interfaceName)
            .Select(x => new HotspotClient { MacAddress = x.MacAddress, IpAddress = x.Address })
            .ToArray();

        return Task.FromResult<IReadOnlyList<HotspotClient>>(clients);
    }


    public async Task Stop(CancellationToken ct = default)
    {
        if (this.stopped)
            return;

        this.stopped = true;
        await client.Deactivate(activeConnectionPath, ct).ConfigureAwait(false);
        logger.HotspotStopped();
        onEnded(this);
    }


    public async ValueTask DisposeAsync() => await this.Stop().ConfigureAwait(false);
}
