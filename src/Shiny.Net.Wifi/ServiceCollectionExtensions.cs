using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Net.Wifi;

namespace Shiny;


public static class WifiServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IWifiManager"/> for scanning, joining and leaving Wi-Fi networks and
    /// for monitoring the currently joined one.
    /// </summary>
    /// <remarks>
    /// <para>Android: manifest needs <c>ACCESS_WIFI_STATE</c>, <c>CHANGE_WIFI_STATE</c> and
    /// <c>ACCESS_FINE_LOCATION</c>; add <c>NEARBY_WIFI_DEVICES</c> for API 33+. Scanning and the
    /// SSID of the joined network both come back empty without location, so call
    /// <see cref="IWifiManager.RequestAccess"/> first.</para>
    /// <para>iOS/Mac Catalyst: needs the <c>Hotspot Configuration</c> and <c>Access WiFi
    /// Information</c> capabilities on the App ID, plus <c>NSLocationWhenInUseUsageDescription</c>
    /// in Info.plist. There is no scanning API - see <see cref="WifiCapabilities"/>.</para>
    /// <para>macOS: needs <c>NSLocationWhenInUseUsageDescription</c>, and
    /// <c>com.apple.security.network.client</c> when sandboxed.</para>
    /// <para>Windows: needs the <c>wiFiControl</c> capability, and <c>radios</c> to power the
    /// adapter.</para>
    /// <para>Linux: reference <c>Shiny.Net.Wifi.Linux</c> instead of this package - it registers a
    /// NetworkManager-backed implementation of the same interfaces.</para>
    /// </remarks>
    public static IServiceCollection AddWifi(this IServiceCollection services)
    {
        // registered by factory rather than by type so nothing here needs reflection under AOT
#if ANDROID
        services.AddSingleton<IWifiManager>(sp => new AndroidWifiManager(
            sp.GetRequiredService<AndroidPlatform>(),
            sp.GetRequiredService<ILogger<AndroidWifiManager>>()
        ));
#elif IOS || MACCATALYST
        services.AddSingleton<IWifiManager>(sp => new AppleWifiManager(
            sp.GetRequiredService<ILogger<AppleWifiManager>>()
        ));
#elif MACOS
        services.AddSingleton<IWifiManager>(sp => new MacOSWifiManager(
            sp.GetRequiredService<ILogger<MacOSWifiManager>>()
        ));
#elif WINDOWS
        services.AddSingleton<IWifiManager>(sp => new WindowsWifiManager(
            sp.GetRequiredService<ILogger<WindowsWifiManager>>()
        ));
#else
        services.AddSingleton<IWifiManager>(sp => new NetWifiManager(
            sp.GetRequiredService<ILogger<NetWifiManager>>()
        ));
#endif
        return services;
    }


    /// <summary>
    /// Registers <see cref="IWifiHotspot"/> for raising an access point.
    /// </summary>
    /// <remarks>
    /// <para>Android raises a local-only hotspot - clients reach this device but get no internet -
    /// and picks the SSID and passphrase itself. Windows shares the machine's current internet
    /// connection and honours the SSID and passphrase you supply. iOS and macOS have no hotspot
    /// API, so the registration succeeds and every call throws
    /// <see cref="WifiNotSupportedException"/>; check <see cref="IWifiHotspot.IsSupported"/>.</para>
    /// </remarks>
    public static IServiceCollection AddWifiHotspot(this IServiceCollection services)
    {
#if ANDROID
        services.AddSingleton<IWifiHotspot>(sp => new AndroidWifiHotspot(
            sp.GetRequiredService<AndroidPlatform>(),
            sp.GetRequiredService<ILogger<AndroidWifiHotspot>>()
        ));
#elif IOS || MACCATALYST
        services.AddSingleton<IWifiHotspot>(_ => new AppleWifiHotspot());
#elif MACOS
        services.AddSingleton<IWifiHotspot>(_ => new MacOSWifiHotspot());
#elif WINDOWS
        services.AddSingleton<IWifiHotspot>(sp => new WindowsWifiHotspot(
            sp.GetRequiredService<ILogger<WindowsWifiHotspot>>()
        ));
#else
        services.AddSingleton<IWifiHotspot>(_ => new NetWifiHotspot());
#endif
        return services;
    }
}
