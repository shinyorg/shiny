using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using Shiny.Net.Discovery;

namespace Shiny.Printers.Network;


/// <summary>Finds printers on the local network - by mDNS browse, or by sweeping the subnet for an open port.</summary>
public interface INetworkPrinterScanner
{
    /// <summary>
    /// Browses the <see cref="KnownNetworkPrinterServiceTypes"/> via mDNS, emitting a
    /// <see cref="DiscoveredNetworkPrinter"/> for each resolved printer. Runs until the subscription is disposed.
    /// </summary>
    IObservable<DiscoveredNetworkPrinter> Scan();

    /// <summary>Browses a specific DNS-SD service type (e.g. a vendor-specific one).</summary>
    IObservable<DiscoveredNetworkPrinter> Scan(string serviceType);

    /// <summary>
    /// Tries a TCP connect to port 9100 (or <see cref="NetworkPrinterSweepConfig.Ports"/>) on every host of the
    /// local subnet, emitting each host that accepts, and completes when the sweep is done. Finds the many
    /// receipt printers that never advertise over mDNS. Any open port 9100 is reported, so a laser printer
    /// answers too.
    /// </summary>
    IObservable<DiscoveredNetworkPrinter> Sweep(NetworkPrinterSweepConfig? config = null);
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
        return Guarded<DiscoveredNetworkPrinter>(async (ob, ct) =>
        {
            await foreach (var result in this.mdns.Browse(serviceType, ct).ConfigureAwait(false))
            {
                var printer = Map(result);
                if (printer != null)
                    ob.OnNext(printer);
            }
        });
    }


    public IObservable<DiscoveredNetworkPrinter> Sweep(NetworkPrinterSweepConfig? config = null)
    {
        config ??= new();
        ArgumentOutOfRangeException.ThrowIfLessThan(config.MaxConcurrency, 1);

        return Guarded<DiscoveredNetworkPrinter>(async (ob, ct) =>
        {
            var targets = SweepTargets(config);
            var gate = new object();

            await Parallel
                .ForEachAsync(
                    targets,
                    new ParallelOptions { MaxDegreeOfParallelism = config.MaxConcurrency, CancellationToken = ct },
                    async (target, token) =>
                    {
                        if (await SubnetSweep.Probe(target.Address, target.Port, config.ProbeTimeout, token).ConfigureAwait(false))
                        {
                            var host = target.Address.ToString();
                            var printer = new DiscoveredNetworkPrinter
                            {
                                Name = host,
                                Host = host,
                                Port = target.Port,
                                ServiceType = string.Empty,
                                Method = NetworkPrinterDiscoveryMethod.PortSweep,
                                Capabilities = config.Capabilities
                            };
                            // probes finish on many threads at once - observers expect one call at a time
                            lock (gate)
                                ob.OnNext(printer);
                        }
                    }
                )
                .ConfigureAwait(false);
        });
    }


    // an async Create that faults after the subscriber has gone leaves the exception on its task, where it
    // surfaces as TaskScheduler.UnobservedTaskException (#1674) - hand it to the observer, which drops it once disposed
    static IObservable<T> Guarded<T>(Func<IObserver<T>, CancellationToken, Task> subscribeAsync) => Observable.Create<T>(async (ob, ct) =>
    {
        try
        {
            await subscribeAsync(ob, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ob.OnError(ex);
        }
    });


    internal static IEnumerable<(IPAddress Address, int Port)> SweepTargets(NetworkPrinterSweepConfig config)
    {
        IEnumerable<IPAddress> addresses;
        if (config.Networks is { } networks)
        {
            addresses = networks.SelectMany(SubnetSweep.Hosts);
        }
        else
        {
            var local = SubnetSweep.LocalNetworks();
            var self = local.Select(x => x.Self).ToHashSet();
            addresses = local.SelectMany(x => SubnetSweep.Hosts(x.Network)).Where(x => !self.Contains(x));
        }

        return addresses
            .Distinct()
            .SelectMany(address => config.Ports.Select(port => (address, port)));
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
