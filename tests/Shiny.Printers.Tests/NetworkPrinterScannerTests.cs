using System.Net;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Shiny.Net.Discovery;
using Shiny.Printers.Network;

namespace Shiny.Printers.Tests;


public class NetworkPrinterScannerTests
{
    static MdnsService Service(string name, int port, params string[] addresses) => new()
    {
        InstanceName = name,
        ServiceType = KnownNetworkPrinterServiceTypes.PdlDatastream,
        HostName = "printer.local",
        Port = port,
        Addresses = addresses.Select(IPAddress.Parse).ToArray(),
        TxtRecords = new Dictionary<string, string> { ["ty"] = "TM-T20" }
    };


    [Fact]
    public void Map_Found_Resolved_Prefers_IPv4()
    {
        var printer = NetworkPrinterScanner.Map(new(MdnsBrowseStatus.Found, Service("Front Counter", 9100, "fe80::1", "192.168.1.50")));

        Assert.NotNull(printer);
        Assert.Equal("Front Counter", printer.Name);
        Assert.Equal("192.168.1.50", printer.Host);
        Assert.Equal(9100, printer.Port);
        Assert.Equal(KnownNetworkPrinterServiceTypes.PdlDatastream, printer.ServiceType);
        Assert.Equal("TM-T20", printer.Properties["ty"]);
    }


    [Fact]
    public void Map_Falls_Back_To_IPv6()
    {
        var printer = NetworkPrinterScanner.Map(new(MdnsBrowseStatus.Found, Service("v6 only", 9100, "fe80::1")));
        Assert.Equal("fe80::1", printer?.Host);
    }


    [Fact]
    public void Map_Ignores_Lost_And_Unresolved()
    {
        Assert.Null(NetworkPrinterScanner.Map(new(MdnsBrowseStatus.Lost, Service("gone", 9100, "192.168.1.50"))));
        Assert.Null(NetworkPrinterScanner.Map(new(MdnsBrowseStatus.Found, Service("no port", 0, "192.168.1.50"))));
        Assert.Null(NetworkPrinterScanner.Map(new(MdnsBrowseStatus.Found, Service("no address", 9100))));
    }


    [Fact]
    public async Task Scan_Merges_Service_Types_And_Dedupes_By_Endpoint()
    {
        // the same printer advertised under both _pdl-datastream and _printer must surface once
        var fake = new FakeMdnsManager(Service("Front Counter", 9100, "192.168.1.50"));
        var scanner = new NetworkPrinterScanner(fake);

        var found = await scanner.Scan().Take(TimeSpan.FromMilliseconds(300)).ToList();

        Assert.Single(found);
        Assert.Equal(KnownNetworkPrinterServiceTypes.All.Count, fake.Browsed.Count);
    }


    [Fact]
    public async Task Disposing_Subscription_Cancels_Browse()
    {
        var fake = new FakeMdnsManager(Service("Front Counter", 9100, "192.168.1.50"));
        var scanner = new NetworkPrinterScanner(fake);

        var first = await scanner.Scan(KnownNetworkPrinterServiceTypes.PdlDatastream).FirstAsync();
        Assert.Equal("Front Counter", first.Name);

        // FirstAsync disposes on the first value - the browse loop must observe the cancellation
        await fake.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }


    sealed class FakeMdnsManager(MdnsService service) : IMdnsManager
    {
        public List<string> Browsed { get; } = [];
        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);


        public async IAsyncEnumerable<MdnsBrowseResult> Browse(MdnsBrowseConfig config, [EnumeratorCancellation] CancellationToken ct = default)
        {
            lock (this.Browsed)
                this.Browsed.Add(config.ServiceType);

            yield return new MdnsBrowseResult(MdnsBrowseStatus.Found, service with { ServiceType = config.ServiceType });

            // like a real browse: never completes on its own
            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            finally
            {
                this.Cancelled.TrySetResult();
            }
        }


        public Task<MdnsService?> Resolve(string instanceName, string serviceType, TimeSpan? timeout = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<IMdnsPublication> Publish(MdnsServiceRegistration registration, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
