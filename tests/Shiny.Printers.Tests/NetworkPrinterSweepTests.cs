using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using Shiny.Net.Discovery;
using Shiny.Printers.Network;

namespace Shiny.Printers.Tests;


public class NetworkPrinterSweepTests
{
    [Fact]
    public void Hosts_Skips_Network_And_Broadcast()
    {
        var hosts = SubnetSweep.Hosts(IPNetwork.Parse("192.168.1.0/24")).ToList();

        Assert.Equal(254, hosts.Count);
        Assert.Equal(IPAddress.Parse("192.168.1.1"), hosts[0]);
        Assert.Equal(IPAddress.Parse("192.168.1.254"), hosts[^1]);
    }


    [Fact]
    public void Hosts_Keeps_Every_Address_In_A_31_Or_32()
    {
        Assert.Equal([IPAddress.Parse("10.0.0.5")], SubnetSweep.Hosts(IPNetwork.Parse("10.0.0.5/32")));
        Assert.Equal(2, SubnetSweep.Hosts(IPNetwork.Parse("10.0.0.4/31")).Count());
    }


    [Fact]
    public void Hosts_Rejects_IPv6()
        => Assert.Throws<ArgumentException>(() => SubnetSweep.Hosts(IPNetwork.Parse("fe80::/120")).ToList());


    [Theory]
    [InlineData("10.1.2.3", 16, "10.1.2.0/24")]   // a wide corporate subnet narrows to the /24 around us
    [InlineData("192.168.1.77", 24, "192.168.1.0/24")]
    [InlineData("192.168.1.77", 28, "192.168.1.64/28")]
    public void ToSweepNetwork_Narrows_Wide_Subnets(string address, int prefix, string expected)
        => Assert.Equal(IPNetwork.Parse(expected), SubnetSweep.ToSweepNetwork(IPAddress.Parse(address), prefix));


    [Fact]
    public void SweepTargets_Crosses_Hosts_And_Ports()
    {
        var targets = NetworkPrinterScanner
            .SweepTargets(new NetworkPrinterSweepConfig
            {
                Networks = [IPNetwork.Parse("10.0.0.4/31"), IPNetwork.Parse("10.0.0.5/32")],
                Ports = [9100, 9101]
            })
            .ToList();

        // 10.0.0.5 is in both networks - probed once per port
        Assert.Equal(4, targets.Count);
        Assert.Contains((IPAddress.Parse("10.0.0.5"), 9101), targets);
    }


    [Fact]
    public async Task Sweep_Finds_Open_Port_And_Completes()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var open = ((IPEndPoint)listener.LocalEndpoint).Port;

        // grab a port that is free right now, so nothing answers on it
        var closedListener = new TcpListener(IPAddress.Loopback, 0);
        closedListener.Start();
        var closed = ((IPEndPoint)closedListener.LocalEndpoint).Port;
        closedListener.Stop();

        try
        {
            var scanner = new NetworkPrinterScanner(new NoMdns());
            var found = await scanner
                .Sweep(new NetworkPrinterSweepConfig
                {
                    Networks = [IPNetwork.Parse("127.0.0.1/32")],
                    Ports = [open, closed],
                    Capabilities = PrinterCapabilities.Paper58mm
                })
                .ToList()
                .Timeout(TimeSpan.FromSeconds(10));

            var printer = Assert.Single(found);
            Assert.Equal("127.0.0.1", printer.Host);
            Assert.Equal(open, printer.Port);
            Assert.Equal(NetworkPrinterDiscoveryMethod.PortSweep, printer.Method);
            Assert.Equal(58, printer.Capabilities.PaperWidthMm);
        }
        finally
        {
            listener.Stop();
        }
    }


    // the sweep never touches mDNS
    sealed class NoMdns : IMdnsManager
    {
        public IAsyncEnumerable<MdnsBrowseResult> Browse(MdnsBrowseConfig config, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MdnsService?> Resolve(string instanceName, string serviceType, TimeSpan? timeout = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IMdnsPublication> Publish(MdnsServiceRegistration registration, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
