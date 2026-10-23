using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Vpn;

namespace Shiny;


// Platform targets only. The plain .NET target is what Shiny.Net.Vpn.Linux builds against, and it
// declares AddVpn itself - the same split as Shiny.Net.Wifi / Shiny.Net.Wifi.Linux.
#if PLATFORM
public static class VpnServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IVpnManager"/> for the operating system's built-in IKEv2 VPN client.
    /// </summary>
    /// <remarks>
    /// <para>iOS, Mac Catalyst and macOS: enable the <c>Personal VPN</c> capability on the App ID and add
    /// the <c>com.apple.developer.networking.vpn.api</c> entitlement with the value <c>allow-vpn</c>.</para>
    /// <para>Android: needs API 30 (Android 11) for anything at all and API 33 for
    /// <see cref="VpnCapabilities.Status"/>; no manifest entries are needed.</para>
    /// <para>Linux: this method does not exist on the plain <c>net10.0</c> target. Reference
    /// <c>Shiny.Net.Vpn.Linux</c>, which declares <c>AddVpn</c> backed by NetworkManager.</para>
    /// </remarks>
    public static IServiceCollection AddVpn(this IServiceCollection services)
    {
#if ANDROID
        services.AddSingleton<IVpnManager, AndroidVpnManager>();
#elif APPLE
        services.AddSingleton<IVpnManager, AppleVpnManager>();
#endif
        return services;
    }
}
#endif
