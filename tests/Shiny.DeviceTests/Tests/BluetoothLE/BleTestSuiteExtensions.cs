namespace Shiny.DeviceTests.Tests;


static class BleTestSuiteExtensions
{
    public static void RequireBluetooth(this TestContext ctx)
    {
        if (ctx.IsApple && ctx.IsSimulator)
            TestContext.Skip("The iOS simulator has no Bluetooth");
    }


    // a peer command whose "skip" value becomes this test's skip
    public static async Task<Dictionary<string, string>> BlePeerAsync(this TestContext ctx, string command, Dictionary<string, string>? args = null)
    {
        var response = await ctx.Host.PeerAsync(command, args, ctx.CancellationToken).ConfigureAwait(false);
        var values = response.Values ?? [];
        if (values.TryGetValue("skip", out var reason))
            TestContext.Skip($"Peer ({ctx.HostInfo.PeerPlatform}): {reason}");

        if (values.TryGetValue("log", out var log) && log.Length > 0)
            ctx.Log($"peer: {log}");

        return values;
    }


    public static async Task<BleHostStats> PeerHostStatsAsync(this TestContext ctx, bool reset = false)
    {
        var values = await ctx
            .BlePeerAsync("ble.host.stats", reset ? new() { ["reset"] = "true" } : null)
            .ConfigureAwait(false);

        return BleHostStats.FromValues(values);
    }
}
