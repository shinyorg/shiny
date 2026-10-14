using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Reactive.Threading.Tasks;
using Shiny.Net.Discovery;
using Shiny.Printers;
using Shiny.Printers.Document;
using Shiny.Printers.Network;
using Shiny.Printers.Protocols.EscPos;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Prints to the runner's fake RAW printer (TCP 9100, see the runner's NetworkPrinterFixture) and asserts on the
/// exact bytes that arrived. The status and sweep tests use the printer-status / subnet-sweep APIs.
/// </summary>
public class NetworkPrinterTests(
    NetworkPrinterManager manager,
    INetworkPrinterScanner scanner,
    IMdnsManager mdns
) : DeviceTestSuite
{
    static readonly byte[] EscPosInit = [0x1B, 0x40];

    public override string Name => "NetworkPrinters";


    protected override void Define()
    {
        this.Test("PrintRaw_BytesArriveVerbatim", async ctx =>
        {
            var host = await this.ResetAsync(ctx);
            var payload = System.Text.Encoding.ASCII.GetBytes($"shiny device test {Guid.NewGuid():N}\n");

            var printer = await manager.Connect(host, DeviceTestConstants.PrinterPort, PrinterCapabilities.Paper80mm, ctx.CancellationToken);
            try
            {
                Assert.True(printer.IsConnected);
                await printer.PrintRaw(payload, ctx.CancellationToken);
            }
            finally
            {
                (printer as IDisposable)?.Dispose();
            }

            var capture = await WaitForBytesAsync(ctx, payload.Length);
            Assert.Equal(1, capture.Connections);
            Assert.Equal(payload, Convert.FromBase64String(capture.Base64Data));
        });


        this.Test("PrintDocument_SendsEncodedEscPos", async ctx =>
        {
            var host = await this.ResetAsync(ctx);
            var document = new PrintDocument()
                .AlignCenter()
                .Bold()
                .Line("Shiny Device Tests")
                .Bold(false)
                .AlignLeft()
                .Line($"Order {Guid.NewGuid():N}")
                .Feed(2)
                .Cut(CutMode.Full);

            // the default protocol for a network printer is ESC/POS - encode it ourselves for the expected bytes
            var expected = new EscPosProtocol().Encode(document, PrinterCapabilities.Paper80mm);

            var printer = await manager.Connect(
                new NetworkPrinterConfig { Host = host, Port = DeviceTestConstants.PrinterPort, Capabilities = PrinterCapabilities.Paper80mm },
                ctx.CancellationToken
            );
            try
            {
                await printer.Print(document, ctx.CancellationToken);
            }
            finally
            {
                (printer as IDisposable)?.Dispose();
            }

            var received = Convert.FromBase64String((await WaitForBytesAsync(ctx, expected.Length)).Base64Data);
            ctx.Log($"{received.Length} bytes received");
            Assert.Equal(expected, received);
            Assert.Equal(EscPosInit, received[..2]);                                       // ESC @
            Assert.True(ContainsSequence(received, [0x1D, 0x56, 0x00]), "no GS V 0 (full cut)");
        });


        this.Test("Connect_ClosedPort_Throws", async ctx =>
        {
            var host = await this.ResetAsync(ctx);
            // nothing listens on the discard port on the runner host
            await Assert.ThrowsAnyAsync<Exception>(() => manager.Connect(
                new NetworkPrinterConfig
                {
                    Host = host,
                    Port = 9,
                    Capabilities = PrinterCapabilities.Paper58mm,
                    ConnectTimeout = TimeSpan.FromSeconds(5)
                },
                ctx.CancellationToken
            ));
        });


        // ── uses the in-progress printer status API (IPrinter.SupportsStatus / GetStatus) ──
        this.Test("GetStatus_AllClear", async ctx =>
        {
            var host = await this.ResetAsync(ctx);
            var printer = await manager.Connect(host, DeviceTestConstants.PrinterPort, PrinterCapabilities.Paper80mm, ctx.CancellationToken);
            try
            {
                Assert.True(printer.SupportsStatus);
                var status = await printer.GetStatus(ctx.CancellationToken);
                ctx.Log(status.ToString());
                Assert.True(status.IsOnline);
                Assert.True(status.CanPrint);
                Assert.False(status.IsPaperOut);
            }
            finally
            {
                (printer as IDisposable)?.Dispose();
            }
        });


        this.Test("GetStatus_PaperOutAndCoverOpen", async ctx =>
        {
            var host = await this.ResetAsync(ctx);

            // n=1 offline (0x08), n=2 cover open (0x04), n=3 no error, n=4 roll paper end (0x60)
            var set = await ctx.Host.SetPrinterStatusAsync("1A161272", ctx.CancellationToken);
            Assert.True(set.IsSuccessStatusCode, $"runner refused the status reply: {(int)set.StatusCode}");

            var printer = await manager.Connect(host, DeviceTestConstants.PrinterPort, PrinterCapabilities.Paper80mm, ctx.CancellationToken);
            try
            {
                var status = await printer.GetStatus(ctx.CancellationToken);
                ctx.Log(status.ToString());
                Assert.False(status.IsOnline);
                Assert.True(status.IsCoverOpen);
                Assert.True(status.IsPaperOut);
                Assert.False(status.CanPrint);
                Assert.False(status.HasError);

                // a status query must not disturb printing on the same connection
                await printer.PrintRaw(new byte[] { 0x41, 0x0A }, ctx.CancellationToken);
            }
            finally
            {
                (printer as IDisposable)?.Dispose();
            }
        });


        // ── uses the in-progress subnet sweep (INetworkPrinterScanner.Sweep) ──
        this.Test("Sweep_FindsRunnerPrinter", async ctx =>
        {
            var host = await this.ResetAsync(ctx);
            var address = await ResolveIPv4Async(host, ctx.CancellationToken);

            var found = await scanner
                .Sweep(new NetworkPrinterSweepConfig
                {
                    Networks = [new IPNetwork(address, 32)],
                    ProbeTimeout = TimeSpan.FromSeconds(2)
                })
                .Timeout(TimeSpan.FromSeconds(20))
                .FirstAsync()
                .ToTask(ctx.CancellationToken);

            ctx.Log($"Swept {found.Host}:{found.Port} ({found.Method})");
            Assert.Equal(address.ToString(), found.Host);
            Assert.Equal(DeviceTestConstants.PrinterPort, found.Port);
            Assert.Equal(NetworkPrinterDiscoveryMethod.PortSweep, found.Method);
        });


        // mDNS self-loop: advertise a RAW printer from this device and let the printer scanner find it
        this.Test("Scan_FindsMdnsAdvertisedPrinter", async ctx =>
        {
            var name = $"Shiny Printer {Guid.NewGuid().ToString("N")[..8]}";
            await using var publication = await mdns.Publish(
                new MdnsServiceRegistration(name, KnownNetworkPrinterServiceTypes.PdlDatastream, DeviceTestConstants.PrinterPort)
                {
                    TxtRecords = new Dictionary<string, string> { ["ty"] = "Shiny Device Test", ["product"] = "(ShinyTest)" }
                },
                ctx.CancellationToken
            );

            var found = await scanner
                .Scan(KnownNetworkPrinterServiceTypes.PdlDatastream)
                .Where(x => x.Name == publication.InstanceName)
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstAsync()
                .ToTask(ctx.CancellationToken);

            ctx.Log($"Scanned {found.Name} {found.Host}:{found.Port}");
            Assert.Equal(DeviceTestConstants.PrinterPort, found.Port);
            Assert.Equal(KnownNetworkPrinterServiceTypes.PdlDatastream, found.ServiceType);
            Assert.Equal(NetworkPrinterDiscoveryMethod.Mdns, found.Method);
            Assert.Equal("Shiny Device Test", found.Properties["ty"]);
        });
    }


    // the runner's host as the app sees it (127.0.0.1 on simulators, 10.0.2.2 on the Android emulator, LAN IP on devices)
    async Task<string> ResetAsync(TestContext ctx)
    {
        ctx.RequireCapability(HostCapability.Server);
        var response = await ctx.Host.DeleteAsync("env/printer", ctx.CancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"runner could not reset the fake printer: {(int)response.StatusCode}");
        return new Uri(ctx.Host.BaseUrl!).Host;
    }


    // the app sends asynchronously to the runner's socket - poll the capture until everything has landed
    static async Task<PrinterCapture> WaitForBytesAsync(TestContext ctx, int length)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        var capture = await GetCaptureAsync(ctx.Host, cts.Token);
        while (Convert.FromBase64String(capture.Base64Data).Length < length)
        {
            if (cts.IsCancellationRequested)
                throw new TimeoutException($"Only {Convert.FromBase64String(capture.Base64Data).Length} of {length} bytes reached the fake printer");

            await Task.Delay(100, CancellationToken.None);
            capture = await GetCaptureAsync(ctx.Host, ctx.CancellationToken);
        }
        return capture;
    }


    static async Task<PrinterCapture> GetCaptureAsync(HostClient host, CancellationToken ct)
    {
        var response = await host.GetAsync("env/printer", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(DeviceTestJsonContext.Default.PrinterCapture, ct))!;
    }


    static async Task<IPAddress> ResolveIPv4Async(string host, CancellationToken ct)
    {
        if (IPAddress.TryParse(host, out var ip))
            return ip;

        var addresses = await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork, ct);
        return addresses.First();
    }


    static bool ContainsSequence(byte[] data, byte[] sequence)
        => data.AsSpan().IndexOf(sequence) >= 0;
}


static class PrinterHostClientExtensions
{
    public static Task<HttpResponseMessage> SetPrinterStatusAsync(this HostClient host, string replyHex, CancellationToken ct)
        => host.PostAsync($"env/printer/status?reply={replyHex}", null, ct);
}
