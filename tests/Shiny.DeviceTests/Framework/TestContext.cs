namespace Shiny.DeviceTests.Framework;


public sealed class TestContext(HostClient host, HostInfo hostInfo, EventHub events, CancellationToken cancellationToken)
{
    readonly List<string> log = [];

    public HostClient Host => host;
    public HostInfo HostInfo => hostInfo;
    public EventHub Events => events;
    public CancellationToken CancellationToken => cancellationToken;

    // "ios", "android" or "maccatalyst"
    public string Platform => hostInfo.Platform;
    public bool IsSimulator => hostInfo.IsSimulator;
    public bool IsApple => this.Platform is "ios" or "maccatalyst";
    public bool IsAndroid => this.Platform == "android";

    public IReadOnlyList<string> LogEntries
    {
        get { lock (this.log) return this.log.ToArray(); }
    }

    public void Log(string message)
    {
        lock (this.log)
            this.log.Add($"{DateTime.Now:HH:mm:ss.fff} {message}");
    }

    public static void Skip(string reason) => throw new SkipException(reason);

    public void SkipUnless(bool condition, string reason)
    {
        if (!condition)
            throw new SkipException(reason);
    }

    public void RequireCapability(HostCapability capability)
    {
        if (!hostInfo.Capabilities.HasFlag(capability))
            throw new SkipException($"The runner cannot provide '{capability}' for {this.Platform}{(this.IsSimulator ? " (simulator)" : "")}");
    }

    public void RequirePlatform(params string[] platforms)
    {
        if (!platforms.Contains(this.Platform, StringComparer.OrdinalIgnoreCase))
            throw new SkipException($"Only runs on {String.Join("/", platforms)}");
    }

    public void RequireRealDevice(string why)
    {
        if (this.IsSimulator)
            throw new SkipException($"Needs a physical device: {why}");
    }

    public async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, string because)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            while (!condition())
                await Task.Delay(100, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out after {timeout.TotalSeconds:0.#}s waiting for {because}");
        }
    }
}
