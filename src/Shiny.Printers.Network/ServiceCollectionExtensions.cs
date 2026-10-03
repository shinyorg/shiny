using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Net.Discovery;
using Shiny.Printers.Network;

namespace Shiny;


public static class NetworkPrintersServiceCollectionExtensions
{
    /// <summary>
    /// Registers network (WiFi / Ethernet) thermal printing: the <see cref="NetworkPrinterManager"/> and an
    /// mDNS-backed <see cref="INetworkPrinterScanner"/>. Registers <see cref="IMdnsManager"/> from
    /// Shiny.Net.Discovery too, unless you already have.
    /// </summary>
    /// <remarks>
    /// iOS / Mac Catalyst: add <c>_pdl-datastream._tcp</c> and <c>_printer._tcp</c> to <c>NSBonjourServices</c>
    /// and set <c>NSLocalNetworkUsageDescription</c> in Info.plist - without them iOS blocks both the browse and
    /// the socket to the printer.
    /// </remarks>
    public static IServiceCollection AddNetworkPrinting(this IServiceCollection services)
    {
        if (!services.HasService<IMdnsManager>())
            services.AddMdns();

        services.TryAddSingleton<NetworkPrinterManager>();
        services.TryAddSingleton<INetworkPrinterScanner, NetworkPrinterScanner>();
        return services;
    }
}
