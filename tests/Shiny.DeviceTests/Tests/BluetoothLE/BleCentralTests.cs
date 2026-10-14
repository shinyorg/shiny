namespace Shiny.DeviceTests.Tests;


/// <summary>
/// This device is the central; the peer device hosts BleTestProfile (ble.host.start). Every write is cross-checked
/// against the peer's own counters, so "the write returned" and "the bytes arrived" are both asserted.
/// </summary>
public class BleCentralTests(BleTestCentral central) : DeviceTestSuite
{
    string? serviceUuid;

    public override string Name => "BleCentral";
    protected override TimeSpan DefaultTimeout => TimeSpan.FromSeconds(120);


    public override async Task SetupAsync(TestContext context)
    {
        context.RequireBluetooth();
        context.RequireCapability(HostCapability.Peer);

        var values = await context.BlePeerAsync("ble.host.start");
        this.serviceUuid = values["serviceUuid"];
        context.Log($"Peer ({context.HostInfo.PeerPlatform}) hosting {this.serviceUuid}, PSM {values["psm"]}");
    }


    public override async Task TeardownAsync(TestContext context)
    {
        await central.DisconnectAsync();
        await context.Host.PeerAsync("ble.host.stop");
    }


    protected override void Define()
    {
        this.Test("Connect_DiscoversProfile", async ctx =>
        {
            var values = await this.Run(ctx, "connect");
            Assert.Equal(BleTestProfile.AllCharacteristics.Length.ToString(), values["characteristics"]);
        });

        this.Test("Read_ReturnsHostValue", async ctx =>
        {
            await ctx.PeerHostStatsAsync(reset: true);
            await this.Run(ctx, "read");

            var stats = await ctx.PeerHostStatsAsync();
            Assert.True(stats.Reads >= 1, "the peer never saw the read");
        });

        this.Test("WriteWithResponse_ReachesHost", async ctx =>
        {
            var values = await this.Run(ctx, "echo");

            var stats = await ctx.PeerHostStatsAsync();
            Assert.Equal(values["echo"], stats.EchoHex);
        });

        this.Test("WriteWithoutResponse_AllPacketsArriveInOrder", async ctx =>
        {
            await ctx.PeerHostStatsAsync(reset: true);
            var values = await this.Run(ctx, "wwr");
            var sent = Int32.Parse(values["packets"]);

            // nothing acknowledges these writes - give the last ones time to land on the peer
            var stats = await ctx.PeerHostStatsAsync();
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (stats.WwrPackets < sent && DateTime.UtcNow < deadline)
            {
                await Task.Delay(500, ctx.CancellationToken);
                stats = await ctx.PeerHostStatsAsync();
            }

            ctx.Log($"Peer received {stats.WwrPackets}/{sent} packets, {stats.WwrBytes} bytes, {stats.WwrOutOfOrder} out of order");
            Assert.Equal(sent, stats.WwrPackets);
            Assert.Equal(Int64.Parse(values["bytes"]), stats.WwrBytes);
            Assert.Equal(0, stats.WwrOutOfOrder);
        });

        this.Test("Notify_DeliversEveryPacketInOrder", async ctx =>
        {
            await this.Run(ctx, "notify");
            await AssertNoHostError(ctx);
        });

        this.Test("Indicate_DeliversEveryPacketInOrder", async ctx =>
        {
            await this.Run(ctx, "indicate");
            await AssertNoHostError(ctx);
        });

        this.Test("FramedMessage_RoundTrips", async ctx =>
        {
            await ctx.PeerHostStatsAsync(reset: true);
            await this.Run(ctx, "message");

            var stats = await ctx.PeerHostStatsAsync();
            Assert.Equal(1, stats.MessagesEchoed);
        });

        this.Test("Mtu_IsNegotiated", ctx => this.Run(ctx, "mtu"));

        this.Test("L2Cap_EchoesStream", async ctx =>
        {
            await ctx.PeerHostStatsAsync(reset: true);
            await this.Run(ctx, "l2cap");

            var stats = await ctx.PeerHostStatsAsync();
            Assert.Equal(BleTestCentral.L2CapPayloadSize, stats.L2CapBytesEchoed);
        });

        this.Test("Reconnect_AfterDisconnect", ctx => this.Run(ctx, "reconnect"));
    }


    Task<Dictionary<string, string>> Run(TestContext ctx, string scenario)
        => central.RunAsync(scenario, this.serviceUuid!, ctx.Log, ctx.CancellationToken);


    static async Task AssertNoHostError(TestContext ctx)
    {
        var stats = await ctx.PeerHostStatsAsync();
        Assert.Null(stats.LastError);
    }
}
