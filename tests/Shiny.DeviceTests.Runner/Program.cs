using Shiny.DeviceTests;
using Shiny.DeviceTests.Runner;
using Shiny.DeviceTests.Runner.Fixtures;

var options = RunnerOptions.Parse(args);
if (options == null)
{
    RunnerOptions.PrintUsage();
    return 2;
}

using var cts = new CancellationTokenSource(options.Timeout);
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
var ct = cts.Token;

var target = await TargetDevice.ResolveAsync(options.Platform, options.Device, options.Agent, ct);
Console.WriteLine($"Target: {target}");

AgentClient? peer = null;
if (options.PeerPlatform != null)
{
    var peerDevice = await TargetDevice.ResolveAsync(options.PeerPlatform, options.PeerDevice, options.PeerAgent, ct);
    peer = new AgentClient(peerDevice);
    Console.WriteLine($"Peer:   {peerDevice}");
}

// ── fixtures + environment endpoints the app calls back into ──
var state = new RunnerState(target, peer);
var builder = WebApplication.CreateSlimBuilder();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.WebHost.UseUrls($"http://0.0.0.0:{options.Port}");
builder.Services.ConfigureHttpJsonOptions(x => x.SerializerOptions.TypeInfoResolverChain.Insert(0, DeviceTestJsonContext.Default));

var web = builder.Build();
web.MapEnvironment(state);
web.MapHttpTransfer(state);
web.MapDataSync(state);
web.MapNetworkPrinter(state);
await web.StartAsync(ct);

var hostUrl = options.HostUrl ?? target.GetRunnerUrl(options.Port);
Console.WriteLine($"Runner: {hostUrl} (capabilities: {state.Capabilities})");

if (options.ServeOnly)
{
    Console.WriteLine("Serving fixtures only - start a run from the app or `maui devflow`. Ctrl+C to stop.");
    try
    {
        await Task.Delay(Timeout.Infinite, ct);
    }
    catch (OperationCanceledException)
    {
    }
    await web.StopAsync();
    return 0;
}

using var agent = new AgentClient(target);
var devices = peer == null ? [target] : new[] { target, peer.Device };

foreach (var device in devices)
{
    var killed = options.Grant && await device.GrantPermissionsAsync(ct);
    if (options.Launch || killed)
        await device.LaunchAsync(ct);
}

await agent.WaitForAgentAsync(TimeSpan.FromSeconds(90), ct);
if (peer != null)
    await peer.WaitForAgentAsync(TimeSpan.FromSeconds(90), ct);

if (options.List)
{
    foreach (var test in await agent.ListAsync(ct))
        Console.WriteLine($"{test.Suite}.{test.Name}");

    await web.StopAsync();
    return 0;
}

// ── run ──
var started = await agent.StartRunAsync(new TestRunRequest(options.Filter, hostUrl), ct);
Console.WriteLine($"Run {started.RunId}: {started.Total} test(s){(options.Filter == null ? "" : $" matching '{options.Filter}'")}");
Console.WriteLine();

using var alertCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
var alertWatcher = options.DismissAlerts ? WatchAlertsAsync(devices, alertCts.Token) : Task.CompletedTask;

TestRunStatus status = new(started.RunId, true, started.Total, 0, 0, 0, null, []);
var printed = 0;
var lastContact = DateTime.UtcNow;
var agentSilenceLimit = TimeSpan.FromMinutes(5);
var finished = false;
try
{
    while (!finished)
    {
        // a suspended app (another app took the foreground) stops answering - wait it out, but not forever
        TestRunStatus? polled = null;
        try
        {
            polled = await agent.GetStatusAsync(ct);
            lastContact = DateTime.UtcNow;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            var silent = DateTime.UtcNow - lastContact;
            if (silent > agentSilenceLimit)
                throw new TimeoutException($"The app's DevFlow agent has not answered for {silent.TotalMinutes:0.#} minutes - is it still in the foreground?", ex);

            Console.WriteLine($"  ! agent not answering ({silent.TotalSeconds:0}s) - is the app in the foreground?");
        }

        if (polled != null)
        {
            status = polled;
            foreach (var result in status.Results.Skip(printed))
                Print(result, options.Verbose);

            printed = status.Results.Count;
            finished = !status.Running && status.RunId == started.RunId;
        }

        if (!finished)
            await Task.Delay(polled == null ? 5000 : 1000, ct);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Cancelled - stopping the run on the device");
    await agent.CancelAsync(CancellationToken.None);
    status = await agent.GetStatusAsync(CancellationToken.None);
}

alertCts.Cancel();
await alertWatcher;

Console.WriteLine();
Console.WriteLine($"{status.Passed} passed, {status.Failed} failed, {status.Skipped} skipped ({status.Results.Count}/{status.Total} ran)");

if (options.JUnitPath != null)
{
    JUnitWriter.Write(options.JUnitPath, target, status);
    Console.WriteLine($"JUnit: {options.JUnitPath}");
}

await web.StopAsync();
return status.Failed > 0 || status.Results.Count < status.Total ? 1 : 0;


static void Print(TestResult result, bool verbose)
{
    var (tag, color) = result.Outcome switch
    {
        TestOutcome.Passed => ("PASS", ConsoleColor.Green),
        TestOutcome.Failed => ("FAIL", ConsoleColor.Red),
        _ => ("SKIP", ConsoleColor.Yellow)
    };
    Console.ForegroundColor = color;
    Console.Write($"  {tag} ");
    Console.ResetColor();
    Console.WriteLine($"{result.Suite}.{result.Name} ({result.DurationMs:0}ms)");

    if (result.Message != null && result.Outcome != TestOutcome.Passed)
        Console.WriteLine($"       {result.Message}");

    if (result.Outcome == TestOutcome.Failed || verbose)
    {
        foreach (var line in result.Log)
            Console.WriteLine($"       | {line}");
    }
    if (result.Outcome == TestOutcome.Failed && verbose && result.StackTrace != null)
        Console.WriteLine(result.StackTrace);
}


// permission prompts the pre-grant can't cover (notifications, Bluetooth) - accept them as they appear
static async Task WatchAlertsAsync(IEnumerable<TargetDevice> devices, CancellationToken ct)
{
    var watchable = devices.Where(x => x.IsSimulator || x.IsMacCatalyst || x.IsAndroid).ToList();
    while (!ct.IsCancellationRequested)
    {
        try
        {
            foreach (var device in watchable)
                await device.DismissAlertAsync(ct);

            await Task.Delay(2000, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ! alert watcher: {ex.Message}");
            return;
        }
    }
}
