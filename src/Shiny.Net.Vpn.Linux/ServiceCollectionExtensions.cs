using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Vpn;

namespace Shiny;


public static class LinuxVpnServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IVpnManager"/> for Linux, backed by NetworkManager's strongSwan plugin over
    /// D-Bus.
    /// </summary>
    /// <remarks>
    /// Needs a running NetworkManager and the strongSwan plugin (<c>network-manager-strongswan</c> on
    /// Debian/Ubuntu, <c>NetworkManager-strongswan</c> on Fedora). Saving and connecting go through
    /// polkit - interactive on a desktop, and in a headless session needing a polkit rule.
    /// </remarks>
    public static IServiceCollection AddVpn(this IServiceCollection services)
    {
        services.AddSingleton<IVpnManager, LinuxVpnManager>();
        return services;
    }
}
