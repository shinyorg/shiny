using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery;

namespace Shiny;


public static class DiscoveryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMdnsManager"/> for browsing, resolving and publishing DNS-SD
    /// services over multicast DNS.
    /// </summary>
    /// <remarks>
    /// <para>iOS/Mac Catalyst/macOS: add every service type you browse for to the
    /// <c>NSBonjourServices</c> array in Info.plist and set <c>NSLocalNetworkUsageDescription</c>,
    /// otherwise browsing silently returns nothing. Because this goes through NSNetService
    /// (Bonjour) rather than raw sockets, the multicast networking entitlement is NOT required.</para>
    /// <para>Android: no runtime permission is needed, but the app manifest must declare
    /// <c>INTERNET</c> and <c>ACCESS_NETWORK_STATE</c>.</para>
    /// <para>Windows/Linux/server .NET: a managed responder binds UDP 5353. Make sure your
    /// firewall allows it.</para>
    /// </remarks>
    public static IServiceCollection AddMdns(this IServiceCollection services)
    {
        // registered by factory rather than by type so nothing here needs reflection under AOT
#if ANDROID
        services.AddSingleton<IMdnsManager>(sp => new AndroidMdnsManager(
            sp.GetRequiredService<ILogger<AndroidMdnsManager>>()
        ));
#elif APPLE
        services.AddSingleton<IMdnsManager>(sp => new AppleMdnsManager(
            sp.GetRequiredService<ILogger<AppleMdnsManager>>()
        ));
#else
        services.AddSingleton<IMdnsManager>(sp => new Shiny.Net.Discovery.Managed.ManagedMdnsManager(
            sp.GetRequiredService<ILogger<Shiny.Net.Discovery.Managed.ManagedMdnsManager>>()
        ));
#endif
        return services;
    }
}
