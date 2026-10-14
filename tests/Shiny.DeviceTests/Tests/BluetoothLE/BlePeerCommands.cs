using Microsoft.Extensions.Logging;

namespace Shiny.DeviceTests.Tests;


// What the other device can ask this one to do in the BLE loop. A SkipException (no Bluetooth, no L2CAP) comes
// back as a "skip" value rather than a failure, and BleTestSuiteExtensions.PeerAsync turns it back into a skip.

public sealed class BleHostStartCommand(BleTestHost host) : IPeerCommand
{
    public string Name => "ble.host.start";

    public async Task<Dictionary<string, string>?> ExecuteAsync(Dictionary<string, string> args, CancellationToken cancellationToken)
    {
        try
        {
            var serviceUuid = await host.StartAsync(cancellationToken).ConfigureAwait(false);
            return new()
            {
                ["serviceUuid"] = serviceUuid,
                ["psm"] = host.Psm.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
        }
        catch (SkipException ex)
        {
            return new() { ["skip"] = ex.Message };
        }
    }
}


public sealed class BleHostStopCommand(BleTestHost host) : IPeerCommand
{
    public string Name => "ble.host.stop";

    public async Task<Dictionary<string, string>?> ExecuteAsync(Dictionary<string, string> args, CancellationToken cancellationToken)
    {
        await host.StopAsync().ConfigureAwait(false);
        return null;
    }
}


public sealed class BleHostStatsCommand(BleTestHost host) : IPeerCommand
{
    public string Name => "ble.host.stats";

    public Task<Dictionary<string, string>?> ExecuteAsync(Dictionary<string, string> args, CancellationToken cancellationToken)
    {
        // "reset=true" zeroes the counters first, so a test can measure just its own traffic
        if (args.TryGetValue("reset", out var reset) && reset == "true")
            host.ResetStats();

        return Task.FromResult<Dictionary<string, string>?>(host.GetStats().ToValues());
    }
}


public sealed class BleCentralRunCommand(BleTestCentral central, ILogger<BleCentralRunCommand> logger) : IPeerCommand
{
    public string Name => "ble.central.run";

    public async Task<Dictionary<string, string>?> ExecuteAsync(Dictionary<string, string> args, CancellationToken cancellationToken)
    {
        var scenario = args["scenario"];
        var serviceUuid = args["serviceUuid"];
        var log = new List<string>();

        // the dispatcher passes no token - bound every scenario so the runner's HTTP call never hangs
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(90));
        try
        {
            var values = await central
                .RunAsync(scenario, serviceUuid, x =>
                {
                    logger.LogInformation("[ble.central.run {Scenario}] {Message}", scenario, x);
                    log.Add(x);
                }, cts.Token)
                .ConfigureAwait(false);

            values["log"] = String.Join(" | ", log);
            return values;
        }
        catch (SkipException ex)
        {
            return new() { ["skip"] = ex.Message };
        }
        catch (Exception ex)
        {
            // carry the central's own log back so the failing test shows what the peer saw
            throw new InvalidOperationException($"{ex.Message} [peer log: {String.Join(" | ", log)}]", ex);
        }
    }
}


public sealed class BleCentralDisconnectCommand(BleTestCentral central) : IPeerCommand
{
    public string Name => "ble.central.disconnect";

    public async Task<Dictionary<string, string>?> ExecuteAsync(Dictionary<string, string> args, CancellationToken cancellationToken)
    {
        await central.DisconnectAsync().ConfigureAwait(false);
        return null;
    }
}
