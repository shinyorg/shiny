using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using Shiny.Net.Discovery;

namespace Shiny.Printers.Network;


/// <summary>Browses the local network for mDNS-advertised printers.</summary>
public interface INetworkPrinterScanner
{
    /// <summary>
    /// Browses the <see cref="KnownNetworkPrinterServiceTypes"/> via mDNS, emitting a
    /// <see cref="DiscoveredNetworkPrinter"/> for each resolved printer. Runs until the subscription is disposed.
    /// </summary>
    IObservable<DiscoveredNetworkPrinter> Scan();

    /// <summary>Browses a specific DNS-SD service type (e.g. a vendor-specific one).</summary>
    IObservable<DiscoveredNetworkPrinter> Scan(string serviceType);
}


/// <summary>Default <see cref="INetworkPrinterScanner"/> backed by Shiny.Net.Discovery's <see cref="IMdnsManager"/>.</summary>
public sealed class NetworkPrinterScanner(IMdnsManager mdns) : INetworkPrinterScanner
{
    readonly IMdnsManager mdns = mdns ?? throw new ArgumentNullException(nameof(mdns));


    public IObservable<DiscoveredNetworkPrinter> Scan()
        => Observable
            .Merge(KnownNetworkPrinterServiceTypes.All.Select(this.Scan))
            .Distinct(p => $"{p.Host}:{p.Port}");


    public IObservable<DiscoveredNetworkPrinter> Scan(string serviceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceType);

        // Disposing the subscription cancels the token, which ends the (otherwise endless) browse.
        return Observable.Create<DiscoveredNetworkPrinter>(async (ob, ct) =>
        {
            await foreach (var result in this.mdns.Browse(serviceType, ct).ConfigureAwait(false))
            {
                var printer = Map(result);
                if (printer != null)
                    ob.OnNext(printer);
            }
        });
    }


    internal static DiscoveredNetworkPrinter? Map(MdnsBrowseResult result)
    {
        if (result.Status != MdnsBrowseStatus.Found || !result.Service.IsResolved)
            return null;

        var address = PreferredAddress(result.Service.Addresses);
        if (address == null)
            return null;

        return new DiscoveredNetworkPrinter
        {
            Name = result.Service.InstanceName,
            Host = address.ToString(),
            Port = result.Service.Port,
            ServiceType = result.Service.ServiceType,
            Properties = result.Service.TxtRecords
        };
    }


    // IPv4 first - plenty of receipt printers run an IPv6 stack that accepts the mDNS answer but not a raw socket
    static IPAddress? PreferredAddress(IReadOnlyList<IPAddress> addresses)
        => addresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork) ?? addresses.FirstOrDefault();
}
