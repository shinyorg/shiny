using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Shiny.DeviceTests.Framework;


/// <summary>
/// Runs the registered suites one test at a time in the background. Started and polled by the runner through
/// the DevFlow extension (see DevFlowTestExtension), or from the app's own Run button.
/// </summary>
public sealed class TestRunner(
    IServiceProvider services,
    EventHub events,
    ILogger<TestRunner> logger
)
{
    static readonly TimeSpan SetupTimeout = TimeSpan.FromMinutes(2);

    readonly object sync = new();
    TestRunStatus status = new(null, false, 0, 0, 0, 0, null, []);
    CancellationTokenSource? cts;

    public event Action<TestRunStatus>? StatusChanged;


    public TestRunStatus Status
    {
        get { lock (this.sync) return this.status; }
    }


    public List<TestInfo> GetTests() => this
        .ResolveSuites()
        .SelectMany(x => x.Tests)
        .Select(x => new TestInfo(x.Suite, x.Name))
        .ToList();


    public TestRunStarted Start(TestRunRequest request)
    {
        lock (this.sync)
        {
            if (this.status.Running)
                throw new InvalidOperationException($"Run {this.status.RunId} is still in progress");

            var suites = this.ResolveSuites()
                .Select(x => (Suite: x, Tests: x.Tests.Where(t => Matches(request.Filter, t)).ToList()))
                .Where(x => x.Tests.Count > 0)
                .ToList();

            var runId = Guid.NewGuid().ToString("N")[..8];
            var total = suites.Sum(x => x.Tests.Count);
            this.cts = new CancellationTokenSource();
            this.status = new TestRunStatus(runId, true, total, 0, 0, 0, null, []);

            var token = this.cts.Token;
            _ = Task.Run(() => this.RunAsync(request, suites, token), token);
            return new TestRunStarted(runId, total);
        }
    }


    public void Cancel() => this.cts?.Cancel();


    async Task RunAsync(TestRunRequest request, List<(DeviceTestSuite Suite, List<DeviceTestCase> Tests)> suites, CancellationToken cancellationToken)
    {
        var host = new HostClient(request.HostUrl);
        var platform = GetPlatform();
        var isSimulator = DeviceInfo.Current.DeviceType == DeviceType.Virtual;

        HostInfo hostInfo;
        try
        {
            hostInfo = await host.TryGetInfoAsync(platform, isSimulator).ConfigureAwait(false)
                ?? new HostInfo(platform, isSimulator, HostCapability.None, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Runner at {Url} unreachable - environment tests will skip", request.HostUrl);
            hostInfo = new HostInfo(platform, isSimulator, HostCapability.None, null);
        }

        foreach (var (suite, tests) in suites)
        {
            // a suite setup that waits on a system dialog nobody accepts must not hang the whole run
            using var setupCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            setupCts.CancelAfter(SetupTimeout);
            var setupContext = new TestContext(host, hostInfo, events, setupCts.Token);
            string? suiteSkip = null;
            string? suiteError = null;
            try
            {
                var setup = suite.SetupAsync(setupContext);
                var winner = await Task.WhenAny(setup, Task.Delay(Timeout.Infinite, setupCts.Token)).ConfigureAwait(false);
                if (winner != setup)
                    throw new TimeoutException($"Suite setup exceeded {SetupTimeout.TotalSeconds:0}s - is a system dialog (permission prompt) waiting on screen?");

                await setup.ConfigureAwait(false);
            }
            catch (SkipException ex)
            {
                suiteSkip = ex.Message;
            }
            catch (Exception ex)
            {
                suiteError = ex.ToString();
            }

            foreach (var test in tests)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (suiteSkip != null)
                    this.Record(new TestResult(test.Suite, test.Name, TestOutcome.Skipped, 0, suiteSkip, null, []));
                else if (suiteError != null)
                    this.Record(new TestResult(test.Suite, test.Name, TestOutcome.Failed, 0, "Suite setup failed", suiteError, []));
                else
                    this.Record(await this.RunTestAsync(host, hostInfo, test, setupContext.LogEntries, cancellationToken).ConfigureAwait(false));
            }

            if (suiteSkip == null && suiteError == null)
            {
                try
                {
                    await suite.TeardownAsync(new TestContext(host, hostInfo, events, cancellationToken)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Teardown of {Suite} failed", suite.Name);
                }
            }
        }

        lock (this.sync)
            this.status = this.status with { Running = false, Current = null };

        this.StatusChanged?.Invoke(this.Status);
    }


    // the suite setup log is carried on every result so what setup saw (access states, skips) is never lost
    async Task<TestResult> RunTestAsync(HostClient host, HostInfo hostInfo, DeviceTestCase test, IReadOnlyList<string> setupLog, CancellationToken runToken)
    {
        lock (this.sync)
            this.status = this.status with { Current = $"{test.Suite}.{test.Name}" };
        this.StatusChanged?.Invoke(this.Status);

        events.Clear();
        using var testCts = CancellationTokenSource.CreateLinkedTokenSource(runToken);
        testCts.CancelAfter(test.Timeout);
        var context = new TestContext(host, hostInfo, events, testCts.Token);
        var sw = Stopwatch.StartNew();

        logger.LogInformation("[DeviceTest] START {Suite}.{Name}", test.Suite, test.Name);
        try
        {
            var run = test.Run(context);
            var winner = await Task.WhenAny(run, Task.Delay(Timeout.Infinite, testCts.Token)).ConfigureAwait(false);
            if (winner != run)
                throw new TimeoutException($"Test exceeded its {test.Timeout.TotalSeconds:0}s timeout");

            await run.ConfigureAwait(false);
            logger.LogInformation("[DeviceTest] PASS {Suite}.{Name}", test.Suite, test.Name);
            return new TestResult(test.Suite, test.Name, TestOutcome.Passed, sw.Elapsed.TotalMilliseconds, null, null, [.. setupLog.Select(x => "[setup] " + x), .. context.LogEntries]);
        }
        catch (SkipException ex)
        {
            logger.LogInformation("[DeviceTest] SKIP {Suite}.{Name}: {Reason}", test.Suite, test.Name, ex.Message);
            return new TestResult(test.Suite, test.Name, TestOutcome.Skipped, sw.Elapsed.TotalMilliseconds, ex.Message, null, [.. setupLog.Select(x => "[setup] " + x), .. context.LogEntries]);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException && testCts.IsCancellationRequested && !runToken.IsCancellationRequested)
                ex = new TimeoutException($"Test exceeded its {test.Timeout.TotalSeconds:0}s timeout", ex);

            logger.LogError(ex, "[DeviceTest] FAIL {Suite}.{Name}", test.Suite, test.Name);
            return new TestResult(test.Suite, test.Name, TestOutcome.Failed, sw.Elapsed.TotalMilliseconds, ex.Message, ex.ToString(), [.. setupLog.Select(x => "[setup] " + x), .. context.LogEntries]);
        }
    }


    void Record(TestResult result)
    {
        lock (this.sync)
        {
            this.status = this.status with
            {
                Passed = this.status.Passed + (result.Outcome == TestOutcome.Passed ? 1 : 0),
                Failed = this.status.Failed + (result.Outcome == TestOutcome.Failed ? 1 : 0),
                Skipped = this.status.Skipped + (result.Outcome == TestOutcome.Skipped ? 1 : 0),
                Results = [.. this.status.Results, result]
            };
        }
        this.StatusChanged?.Invoke(this.Status);
    }


    IEnumerable<DeviceTestSuite> ResolveSuites() => services
        .GetServices<DeviceTestSuite>()
        .OrderBy(x => x.Name, StringComparer.Ordinal);


    // filter: comma separated, each matched case-insensitively against "Suite" or "Suite.Test" (prefix)
    static bool Matches(string? filter, DeviceTestCase test)
    {
        if (String.IsNullOrWhiteSpace(filter))
            return true;

        var fullName = $"{test.Suite}.{test.Name}";
        return filter
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(f => fullName.StartsWith(f, StringComparison.OrdinalIgnoreCase));
    }


    static string GetPlatform()
    {
#if ANDROID
        return "android";
#elif MACCATALYST
        return "maccatalyst";
#else
        return "ios";
#endif
    }
}
