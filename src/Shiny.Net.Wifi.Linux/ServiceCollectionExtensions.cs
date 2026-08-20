using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Net.Wifi;

namespace Shiny;


public static class LinuxWifiServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IWifiManager"/> for Linux, backed by NetworkManager over D-Bus.
    /// </summary>
    /// <remarks>
    /// Needs a running NetworkManager. Reading and scanning are unprivileged; connecting,
    /// disconnecting and toggling the radio go through polkit - interactive on a desktop, and in a
    /// headless session needing a rule for <c>org.freedesktop.NetworkManager.network-control</c>.
    /// </remarks>
    public static IServiceCollection AddWifi(this IServiceCollection services)
    {
        services.AddSingleton<IWifiManager>(sp => new LinuxWifiManager(
            sp.GetRequiredService<ILogger<LinuxWifiManager>>()
        ));
        return services;
    }


    /// <summary>
    /// Registers <see cref="IWifiHotspot"/> for Linux, backed by NetworkManager AP mode with
    /// <c>ipv4.method=shared</c> for DHCP and NAT.
    /// </summary>
    /// <remarks>
    /// Unlike the mobile platforms this honours the SSID, passphrase and band you supply, and can
    /// enumerate connected clients from the kernel neighbour table.
    /// </remarks>
    public static IServiceCollection AddWifiHotspot(this IServiceCollection services)
    {
        services.AddSingleton<IWifiHotspot>(sp => new LinuxWifiHotspot(
            sp.GetRequiredService<ILogger<LinuxWifiHotspot>>()
        ));
        return services;
    }


    /// <summary>
    /// Registers <see cref="IAirplaneMode"/> for Linux - NetworkManager's wireless and WWAN kill
    /// switches, which is what desktop environments label airplane mode.
    /// </summary>
    public static IServiceCollection AddAirplaneMode(this IServiceCollection services)
    {
        services.AddSingleton<IAirplaneMode>(sp => new LinuxAirplaneMode(
            sp.GetRequiredService<ILogger<LinuxAirplaneMode>>()
        ));
        return services;
    }
}
