using System.Runtime.Versioning;
using Android.Content;
using Android.Net.Wifi;
using Microsoft.Extensions.Logging;
using NativeWifiManager = Android.Net.Wifi.WifiManager;

namespace Shiny.Net.Wifi;


/// <summary>
/// Android's local-only hotspot - an access point other devices can join to reach this one, with
/// no route to the internet.
/// </summary>
/// <remarks>
/// <para>This is the only hotspot a normal app can raise. Real tethering (sharing the mobile data
/// connection) lives behind <c>TETHER_PRIVILEGED</c>, a signature permission, and is not reachable
/// from an app store build at any API level.</para>
/// <para>The OS generates the SSID and passphrase and there is no supported way to choose them -
/// <c>SoftApConfiguration.Builder</c> exposes only the channel to non-system apps. Read them back
/// off <see cref="IHotspotSession.Info"/> and show them to the user, which is the intended flow.</para>
/// <para>The hotspot lives exactly as long as the reservation, so disposing the session (or the
/// process exiting) brings it down. Needs <c>CHANGE_WIFI_STATE</c> plus location, or
/// <c>NEARBY_WIFI_DEVICES</c> from API 33.</para>
/// </remarks>
public class AndroidWifiHotspot(
    AndroidPlatform platform,
    ILogger<AndroidWifiHotspot> logger
) : AbstractWifiHotspot
{
    AndroidHotspotSession? current;

    public override bool IsSupported => true;


    protected override async Task<IHotspotSession> StartNative(HotspotConfiguration? config, CancellationToken ct)
    {
        if (config?.Ssid != null || config?.Passphrase != null)
            logger.HotspotConfigurationIgnored();

        var native = platform.GetSystemService<NativeWifiManager>(Context.WifiService);
        var tcs = new TaskCompletionSource<NativeWifiManager.LocalOnlyHotspotReservation>();

        var callback = new LocalOnlyHotspotCallback(
            reservation => tcs.TrySetResult(reservation),
            error => tcs.TrySetException(new WifiException($"Android refused to start the local-only hotspot - {error}")),
            this.OnHotspotStopped
        );

        native.StartLocalOnlyHotspot(callback, null);

        using (ct.Register(() => tcs.TrySetCanceled(ct)))
        {
            var reservation = await tcs.Task.ConfigureAwait(false);
            var info = ToInfo(reservation);
            logger.HotspotStarted(info.Ssid);

            this.current = new AndroidHotspotSession(reservation, info, this.OnSessionEnded, logger);
            return this.current;
        }
    }


    /// <remarks>
    /// The OS tears a local-only hotspot down on its own - when the last client has been gone long
    /// enough, or when the user switches Wi-Fi back on. Without this the session would keep
    /// reporting IsRunning for an access point that no longer exists.
    /// </remarks>
    void OnHotspotStopped()
    {
        var ended = Interlocked.Exchange(ref this.current, null);
        if (ended == null)
            return;

        ended.MarkStopped();
        logger.HotspotStopped();
        this.OnSessionEnded(ended);
    }


    static HotspotInfo ToInfo(NativeWifiManager.LocalOnlyHotspotReservation reservation)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            return ModernInfo(reservation);

        return LegacyInfo(reservation);
    }


    /// <remarks>
    /// SoftApConfiguration.Ssid was obsoleted in API 33 in favour of the WifiSsid object, which
    /// carries raw bytes rather than a string. The string form is the only one that spans 30-32.
    /// </remarks>
    [SupportedOSPlatform("android30.0")]
#pragma warning disable CA1422
    static HotspotInfo ModernInfo(NativeWifiManager.LocalOnlyHotspotReservation reservation)
    {
        var soft = reservation.SoftApConfiguration;
        return new HotspotInfo
        {
            Ssid = soft?.Ssid ?? String.Empty,
            Passphrase = soft?.Passphrase,
            Security = ToSecurity(soft)
        };
    }
#pragma warning restore CA1422


#pragma warning disable CA1422 // LocalOnlyHotspotReservation.WifiConfiguration is deprecated in API 30+, but it is the only shape below 30
    static HotspotInfo LegacyInfo(NativeWifiManager.LocalOnlyHotspotReservation reservation)
    {
        var legacy = reservation.WifiConfiguration;
        var ssid = legacy?.Ssid ?? String.Empty;
        var key = legacy?.PreSharedKey;

        return new HotspotInfo
        {
            // the legacy shape quotes both, the way WifiConfiguration does everywhere else
            Ssid = Unquote(ssid),
            Passphrase = key == null ? null : Unquote(key),
            Security = key == null ? WifiSecurity.Open : WifiSecurity.Wpa2Psk
        };
    }
#pragma warning restore CA1422


    static string Unquote(string value)
        => value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;


    [SupportedOSPlatform("android30.0")]
    static WifiSecurity ToSecurity(SoftApConfiguration? config)
    {
        if (config == null)
            return WifiSecurity.Unknown;

        var type = (SoftApConfigurationSecurityType)config.SecurityType;

        // OWE only became a soft-AP option in API 33, so its two constants cannot be named in a
        // switch that also has to compile for 30-32
        if (OperatingSystem.IsAndroidVersionAtLeast(33) && IsOwe(type))
            return WifiSecurity.Owe;

        return type switch
        {
            SoftApConfigurationSecurityType.Open => WifiSecurity.Open,
            SoftApConfigurationSecurityType.Wpa2Psk => WifiSecurity.Wpa2Psk,
            SoftApConfigurationSecurityType.Wpa3Sae => WifiSecurity.Wpa3Psk,
            SoftApConfigurationSecurityType.Wpa3SaeTransition => WifiSecurity.Wpa3Psk,
            _ => WifiSecurity.Unknown
        };
    }


    [SupportedOSPlatform("android33.0")]
    static bool IsOwe(SoftApConfigurationSecurityType type)
        => type is SoftApConfigurationSecurityType.Wpa3Owe or SoftApConfigurationSecurityType.Wpa3OweTransition;
}


class AndroidHotspotSession(
    NativeWifiManager.LocalOnlyHotspotReservation reservation,
    HotspotInfo info,
    Action<IHotspotSession> onEnded,
    ILogger logger
) : IHotspotSession
{
    bool stopped;

    public HotspotInfo Info => info;
    public bool IsRunning => !this.stopped;


    /// <summary>Records that the OS stopped the hotspot without going through <see cref="Stop"/>.</summary>
    internal void MarkStopped() => this.stopped = true;


    /// <remarks>
    /// Android has never exposed the clients of a local-only hotspot. The ARP table under
    /// /proc/net that apps used to read for this has been unreadable since Android 10.
    /// </remarks>
    public Task<IReadOnlyList<HotspotClient>> GetClients(CancellationToken ct = default)
        => throw WifiNotSupportedException.For(
            WifiCapabilities.HotspotClients,
            "Android exposes no client list for a local-only hotspot, and has blocked the /proc/net ARP table apps previously read since Android 10"
        );


    public Task Stop(CancellationToken ct = default)
    {
        if (this.stopped)
            return Task.CompletedTask;

        this.stopped = true;
        reservation.Close();
        reservation.Dispose();
        logger.HotspotStopped();
        onEnded(this);
        return Task.CompletedTask;
    }


    public async ValueTask DisposeAsync() => await this.Stop().ConfigureAwait(false);
}


class LocalOnlyHotspotCallback(
    Action<NativeWifiManager.LocalOnlyHotspotReservation> onStarted,
    Action<LocalOnlyHotspotCallbackErrorCode> onFailed,
    Action onStopped
) : NativeWifiManager.LocalOnlyHotspotCallback
{
    public override void OnStarted(NativeWifiManager.LocalOnlyHotspotReservation? reservation)
    {
        if (reservation != null)
            onStarted(reservation);
    }

    public override void OnFailed(LocalOnlyHotspotCallbackErrorCode reason) => onFailed(reason);
    public override void OnStopped() => onStopped();
}
