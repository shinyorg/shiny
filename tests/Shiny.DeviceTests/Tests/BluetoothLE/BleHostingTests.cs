namespace Shiny.DeviceTests.Tests;


/// <summary>
/// This device hosts BleTestProfile; the peer device is the central and runs each scenario (ble.central.run).
/// The same scenarios as BleCentral with the roles swapped, asserted against this device's own counters.
/// </summary>
public class BleHostingTests(BleTestHost host) : DeviceTestSuite
{
    string? serviceUuid;

    public override string Name => "BleHosting";
    protected override TimeSpan DefaultTimeout => TimeSpan.FromSeconds(120);


    public override async Task SetupAsync(TestContext context)
    {
        context.RequireBluetooth();
        context.RequireCapability(HostCapability.Peer);

        this.serviceUuid = await host.StartAsync(context.CancellationToken);
        context.Log($"Hosting {this.serviceUuid}, PSM {host.Psm}");
    }


    public override async Task TeardownAsync(TestContext context)
    {
        try
        {
            await context.Host.PeerAsync("ble.central.disconnect");
        }
        finally
        {
            await host.StopAsync();
        }
    }


    protected override void Define()
    {
        this.Test("Central_DiscoversHostedProfile", async ctx =>
        {
            var values = await this.PeerRun(ctx, "connect");
            Assert.Equal(BleTestProfile.AllCharacteristics.Length.ToString(), values["characteristics"]);
        });

        this.Test("Read_ServedToCentral", async ctx =>
        {
            host.ResetStats();
            await this.PeerRun(ctx, "read");
            Assert.True(host.GetStats().Reads >= 1, "the read handler never ran");
        });

        this.Test("WriteWithResponse_Received", async ctx =>
        {
            var values = await this.PeerRun(ctx, "echo");
            Assert.Equal(values["echo"], host.GetStats().EchoHex);
        });

        this.Test("WriteWithoutResponse_AllPacketsReceivedInOrder", async ctx =>
        {
            host.ResetStats();
            var values = await this.PeerRun(ctx, "wwr");
            var sent = Int32.Parse(values["packets"]);

            await ctx.WaitUntilAsync(() => host.GetStats().WwrPackets >= sent, TimeSpan.FromSeconds(15), $"{sent} write-without-response packets");

            var stats = host.GetStats();
            Assert.Equal(sent, stats.WwrPackets);
            Assert.Equal(Int64.Parse(values["bytes"]), stats.WwrBytes);
            Assert.Equal(0, stats.WwrOutOfOrder);
        });

        this.Test("Notify_SentToSubscriber", async ctx =>
        {
            host.ResetStats();
            await this.PeerRun(ctx, "notify");

            var stats = host.GetStats();
            Assert.Null(stats.LastError);
            Assert.Equal(BleTestCentral.NotifyCount, stats.NotificationsSent);
        });

        this.Test("Indicate_SentToSubscriber", async ctx =>
        {
            host.ResetStats();
            await this.PeerRun(ctx, "indicate");

            var stats = host.GetStats();
            Assert.Null(stats.LastError);
            Assert.Equal(BleTestCentral.IndicateCount, stats.NotificationsSent);
        });

        this.Test("FramedMessage_ReassembledAndEchoed", async ctx =>
        {
            host.ResetStats();
            await this.PeerRun(ctx, "message");
            Assert.Equal(1, host.GetStats().MessagesEchoed);
        });

        this.Test("Mtu_IsNegotiated", ctx => this.PeerRun(ctx, "mtu"));

        this.Test("L2Cap_ServesStream", async ctx =>
        {
            ctx.SkipUnless(host.Psm != 0, "This device could not publish an L2CAP PSM");

            host.ResetStats();
            await this.PeerRun(ctx, "l2cap");
            Assert.Equal(BleTestCentral.L2CapPayloadSize, host.GetStats().L2CapBytesEchoed);
        });

        this.Test("Reconnect_AfterDisconnect", ctx => this.PeerRun(ctx, "reconnect"));
    }


    Task<Dictionary<string, string>> PeerRun(TestContext ctx, string scenario) => ctx.BlePeerAsync(
        "ble.central.run",
        new()
        {
            ["scenario"] = scenario,
            ["serviceUuid"] = this.serviceUuid!
        }
    );
}
