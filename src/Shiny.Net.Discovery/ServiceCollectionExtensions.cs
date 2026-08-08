using System.Net;
using System.Net.Http;
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


    /// <summary>
    /// Registers <see cref="ISsdpManager"/> for discovering and advertising UPnP devices over SSDP.
    /// </summary>
    /// <remarks>
    /// <para>Unlike <see cref="AddMdns"/> this is managed on every platform - no OS exposes an
    /// SSDP API - so it uses raw multicast and inherits the platform requirements that comes with:
    /// the Apple-granted <c>com.apple.developer.networking.multicast</c> entitlement on iOS,
    /// <c>CHANGE_WIFI_MULTICAST_STATE</c> plus <c>ACCESS_LOCAL_NETWORK</c> (Android 17+) on
    /// Android, <c>privateNetworkClientServer</c> for packaged Windows apps, and the sandbox
    /// network entitlements on macOS/Mac Catalyst.</para>
    /// <para>Device descriptions are fetched through a named HttpClient
    /// (<see cref="SsdpConstants.HttpClientName"/>) which you can reconfigure.</para>
    /// </remarks>
    public static IServiceCollection AddSsdp(this IServiceCollection services)
    {
        AddDescriptionHttpClient(services, SsdpConstants.HttpClientName);

        services.AddSingleton<ISsdpManager>(sp => new Shiny.Net.Discovery.Managed.Ssdp.ManagedSsdpManager(
            sp.GetRequiredService<ILogger<Shiny.Net.Discovery.Managed.Ssdp.ManagedSsdpManager>>(),
            sp.GetRequiredService<IHttpClientFactory>()
        ));
        return services;
    }


    /// <summary>
    /// Registers <see cref="IWsDiscoveryManager"/> for discovering and advertising WS-Discovery
    /// target services - ONVIF cameras, WSD printers and scanners, Windows machines.
    /// </summary>
    /// <remarks>
    /// Managed on every platform and subject to the same raw-multicast platform requirements as
    /// <see cref="AddSsdp"/>. Both the 2005 (ONVIF/Windows) and 2009 (OASIS) profiles are spoken
    /// by default.
    /// </remarks>
    public static IServiceCollection AddWsDiscovery(this IServiceCollection services)
    {
        services.AddSingleton<IWsDiscoveryManager>(sp => new Shiny.Net.Discovery.Managed.WsDiscovery.ManagedWsDiscoveryManager(
            sp.GetRequiredService<ILogger<Shiny.Net.Discovery.Managed.WsDiscovery.ManagedWsDiscoveryManager>>()
        ));
        return services;
    }


    /// <summary>
    /// Configures the HttpClient used to fetch metadata from a discovered device.
    /// </summary>
    /// <remarks>
    /// Redirects and cookies are off deliberately. The URL comes from an unauthenticated datagram,
    /// and following a redirect off it is the easiest way to turn discovery into a request forgery
    /// primitive. Decompression is on because a handful of devices gzip their descriptions.
    /// </remarks>
    static void AddDescriptionHttpClient(IServiceCollection services, string name)
        => services
            .AddHttpClient(name)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
}
