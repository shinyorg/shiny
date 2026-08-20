using System.Net;
using Microsoft.Extensions.Logging;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

namespace Shiny.Net.Wifi;


/// <summary>
/// Windows tethering through NetworkOperatorTetheringManager - a real hotspot that shares the
/// machine's current internet connection, not a local-only one.
/// </summary>
/// <remarks>
/// Because it shares a connection, it needs one to share: the manager is built from the active
/// internet connection profile, and starting with the machine offline fails. Group policy, the
/// hardware and the SKU can each veto tethering independently, which is why the capability is
/// checked before the attempt rather than after.
/// </remarks>
public class WindowsWifiHotspot(ILogger<WindowsWifiHotspot> logger) : AbstractWifiHotspot
{
    public override bool IsSupported => true;


    protected override async Task<IHotspotSession> StartNative(HotspotConfiguration? config, CancellationToken ct)
    {
        var profile = NetworkInformation.GetInternetConnectionProfile()
            ?? throw new WifiException("Windows tethering shares an existing internet connection and this machine has none");

        var capability = NetworkOperatorTetheringManager.GetTetheringCapabilityFromConnectionProfile(profile);
        if (capability != TetheringCapability.Enabled)
            throw new WifiNotSupportedException($"Tethering is unavailable on this machine - {capability}");

        var manager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);

        if (config?.Ssid != null || config?.Passphrase != null || config?.Band != WifiBand.Unknown)
            await Configure(manager, config!, ct).ConfigureAwait(false);

        if (manager.TetheringOperationalState != TetheringOperationalState.On)
        {
            var result = await manager.StartTetheringAsync().AsTask(ct).ConfigureAwait(false);
            if (result.Status != TetheringOperationStatus.Success)
                throw new WifiException($"Could not start the hotspot - {result.Status}. {result.AdditionalErrorMessage}");
        }

        var running = manager.GetCurrentAccessPointConfiguration();
        var info = new HotspotInfo
        {
            Ssid = running.Ssid,
            Passphrase = running.Passphrase,
            // Windows tethering is always WPA2-PSK; the API has no way to open the network
            Security = WifiSecurity.Wpa2Psk,
            Band = ToBand(running.Band)
        };

        logger.HotspotStarted(info.Ssid);
        return new WindowsHotspotSession(manager, info, this.OnSessionEnded, logger);
    }


    static async Task Configure(NetworkOperatorTetheringManager manager, HotspotConfiguration config, CancellationToken ct)
    {
        // the running configuration is the starting point - passing a fresh object with only the
        // SSID set would blank the passphrase, which Windows rejects
        var apConfig = manager.GetCurrentAccessPointConfiguration();

        if (config.Ssid != null)
            apConfig.Ssid = config.Ssid;

        if (config.Passphrase != null)
            apConfig.Passphrase = config.Passphrase;

        var band = ToTetheringBand(config.Band);
        if (band != null && apConfig.IsBandSupported(band.Value))
            apConfig.Band = band.Value;

        await manager.ConfigureAccessPointAsync(apConfig).AsTask(ct).ConfigureAwait(false);
    }


    // Windows tethering has no 6 GHz option, so a 6 GHz request falls through to the platform default
    static TetheringWiFiBand? ToTetheringBand(WifiBand band) => band switch
    {
        WifiBand.TwoPointFourGhz => TetheringWiFiBand.TwoPointFourGigahertz,
        WifiBand.FiveGhz => TetheringWiFiBand.FiveGigahertz,
        _ => null
    };


    static WifiBand ToBand(TetheringWiFiBand band) => band switch
    {
        TetheringWiFiBand.TwoPointFourGigahertz => WifiBand.TwoPointFourGhz,
        TetheringWiFiBand.FiveGigahertz => WifiBand.FiveGhz,
        _ => WifiBand.Unknown
    };
}


class WindowsHotspotSession(
    NetworkOperatorTetheringManager manager,
    HotspotInfo info,
    Action<IHotspotSession> onEnded,
    ILogger logger
) : IHotspotSession
{
    bool stopped;

    public HotspotInfo Info => info;
    public bool IsRunning => !this.stopped && manager.TetheringOperationalState == TetheringOperationalState.On;


    public Task<IReadOnlyList<HotspotClient>> GetClients(CancellationToken ct = default)
    {
        var clients = manager
            .GetTetheringClients()
            .Select(x => new HotspotClient
            {
                MacAddress = x.MacAddress,
                // a client shows up with its MAC before DHCP completes, so the address list is
                // often empty for a second or two after it associates
                IpAddress = x.HostNames
                    .Select(h => IPAddress.TryParse(h.CanonicalName, out var parsed) ? parsed : null)
                    .FirstOrDefault(h => h != null),
                HostName = x.HostNames.FirstOrDefault(h => !IPAddress.TryParse(h.CanonicalName, out _))?.CanonicalName
            })
            .ToArray();

        return Task.FromResult<IReadOnlyList<HotspotClient>>(clients);
    }


    public async Task Stop(CancellationToken ct = default)
    {
        if (this.stopped)
            return;

        this.stopped = true;
        var result = await manager.StopTetheringAsync().AsTask(ct).ConfigureAwait(false);
        if (result.Status != TetheringOperationStatus.Success)
            throw new WifiException($"Could not stop the hotspot - {result.Status}. {result.AdditionalErrorMessage}");

        logger.HotspotStopped();
        onEnded(this);
    }


    public async ValueTask DisposeAsync() => await this.Stop().ConfigureAwait(false);
}
