# Shiny.Jobs API Reference

## Installation

```xml
<PackageReference Include="Shiny.Jobs" />
```

Project dependencies (pulled in automatically):

- `Shiny.Core`

Android additionally uses:

- `Xamarin.AndroidX.Work.Runtime`

## Namespaces

```csharp
using Shiny.Jobs;   // job types
using Shiny;         // AddJob<T>() extension
```

---

## Interfaces

### IJob

The contract every job implements.

```csharp
namespace Shiny.Jobs;

public interface IJob
{
    /// <summary>Executes the job's work.</summary>
    /// <param name="cancelToken">Cancellation token to observe.</param>
    Task Run(CancellationToken cancelToken);
}
```

The job receives only a `CancellationToken`. There is no `JobInfo` argument — inject configuration through the constructor.

### IJobManager

The primary service for executing and querying registered jobs. Injected via DI as `IJobManager` (concrete impl is the platform `JobManager`, which derives from `AbstractJobManager`).

```csharp
namespace Shiny.Jobs;

public interface IJobManager
{
    /// <summary>
    /// Runs a single registered job by its CLR type. The job runs normally (inline).
    /// </summary>
    /// <param name="jobType">The registered IJob implementation type.</param>
    /// <param name="cancellationToken">Token used to cancel the running job.</param>
    Task<JobRunResult> RunJob(Type jobType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Force-runs all registered jobs and returns the result for each.
    /// </summary>
    /// <param name="cancelToken">Token used to cancel running jobs.</param>
    /// <param name="runSequentially">When true, jobs run one after another. When false (default), they run concurrently.</param>
    Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default, bool runSequentially = false);

    /// <summary>Gets all currently registered jobs keyed by their CLR type.</summary>
    IReadOnlyDictionary<Type, JobRegistration> GetJobs();
}
```

### AbstractJobManager (additional surface)

The platform managers derive from `AbstractJobManager`, which also exposes:

```csharp
namespace Shiny.Jobs;

public abstract class AbstractJobManager : IJobManager
{
    /// <summary>Indicates whether a RunAll() batch is currently running.</summary>
    public bool IsRunning { get; }

    /// <summary>The registrar that owns the set of registered jobs.</summary>
    protected JobRegistrar Registrar { get; }

    /// <summary>Requests the platform permissions required to schedule background jobs.</summary>
    public abstract Task<AccessState> RequestAccess();
}
```

To call `RequestAccess()` or read `IsRunning`, resolve the concrete `JobManager` (or cast `IJobManager` to `AbstractJobManager`).

---

## Records

### JobRegistration

Describes a registered job and the constraints under which it runs. The job's `JobType` is the unique identity.

```csharp
namespace Shiny.Jobs;

public record JobRegistration(
    Type JobType,
    bool RunOnForeground = false,
    InternetAccess RequiredInternetAccess = InternetAccess.None,
    bool DeviceCharging = false,
    bool BatteryNotLow = false
);
```

### JobRunResult

The outcome of a single job execution.

```csharp
namespace Shiny.Jobs;

public record JobRunResult(
    JobRegistration? Job,
    Exception? Exception
)
{
    public bool Success => this.Exception == null;
}
```

---

## Fluent Configuration

`JobRegistrationExtensions` provides a small fluent surface used inside the `AddJob<T>` configure delegate:

```csharp
namespace Shiny.Jobs;

public static class JobRegistrationExtensions
{
    public static JobRegistration WithForeground(this JobRegistration reg, bool value = true);
    public static JobRegistration WithInternet(this JobRegistration reg, InternetAccess access);
    public static JobRegistration WithCharging(this JobRegistration reg, bool value = true);
    public static JobRegistration WithBatteryNotLow(this JobRegistration reg, bool value = true);
}
```

---

## Abstract Base Classes

### Job

A convenience base class for `IJob` that adds a minimum interval between runs.

```csharp
namespace Shiny.Jobs;

public abstract class Job : IJob
{
    protected ILogger Logger { get; }
    protected Job(ILogger logger);

    /// <summary>The actual work performed by the job. Override this instead of Run.</summary>
    protected abstract Task RunJob(CancellationToken cancelToken);

    /// <summary>Last run time of this job. In-memory, cleared on process restart.</summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>If set, the job will skip a run that occurs sooner than this delta since the previous run.</summary>
    public TimeSpan? MinimumTime { get; set; }
}
```

**Behavior:** When `MinimumTime` is set, the base class checks whether enough time has elapsed since `LastRunTime` before invoking `RunJob(...)`. If the minimum interval has not elapsed, the job is silently skipped. State is in-memory only (does not survive process restart).

---

## Registration

### JobRegistrar

Collects job registrations during host configuration. Normally callers do not interact with it directly — the `AddJob<TJob>` extension obtains the registrar from the service collection and calls `Register<TJob>(configure)`.

```csharp
namespace Shiny.Jobs;

public sealed class JobRegistrar
{
    /// <summary>All jobs registered so far, keyed by their CLR type.</summary>
    public IReadOnlyDictionary<Type, JobRegistration> Jobs { get; }

    /// <summary>Registers the job type and stores the registration. Returns the registrar for fluent chaining.</summary>
    public JobRegistrar Register<TJob>(Func<JobRegistration, JobRegistration>? configure = null)
        where TJob : class, IJob;
}
```

### ServiceCollectionExtensions

Defined in the `Shiny` namespace.

```csharp
namespace Shiny;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register a singleton job with optional fluent configuration.
    /// On first call this also registers the job-manager infrastructure
    /// (and, on platform builds, the foreground JobLifecycleTask plus battery/connectivity).
    /// </summary>
    public static IServiceCollection AddJob<TJob>(
        this IServiceCollection services,
        Func<JobRegistration, JobRegistration>? configure = null
    ) where TJob : class, IJob;
}
```

Example:

```csharp
services.AddJob<MySyncJob>(r => r.WithForeground().WithInternet(InternetAccess.Any));
```

---

## Enums

### InternetAccess

```csharp
namespace Shiny.Jobs;

public enum InternetAccess
{
    None = 0,        // No network requirement
    Any = 1,         // Any internet connection (WiFi, cellular, etc.)
    Unmetered = 2    // Unmetered connection only (typically WiFi)
}
```

### JobState

Internal enum used for log lifecycle events.

```csharp
namespace Shiny.Jobs;

public enum JobState { Start, Finish, Error }
```

### AccessState

Defined in `Shiny` (from `Shiny.Core`). Returned by `AbstractJobManager.RequestAccess()`.

```csharp
namespace Shiny;

public enum AccessState
{
    Unknown,
    NotSupported,
    NotSetup,
    Disabled,
    Restricted,
    Denied,
    Available
}
```

---

## Usage Examples

### Basic Job Implementation

```csharp
using Shiny.Jobs;

public class DataSyncJob(IMyApiService api, IMyLocalDb db) : IJob
{
    public async Task Run(CancellationToken cancelToken)
    {
        var pending = await db.GetPendingUploads(cancelToken);
        foreach (var item in pending)
        {
            cancelToken.ThrowIfCancellationRequested();
            await api.Upload(item, cancelToken);
            await db.MarkUploaded(item.Id, cancelToken);
        }
    }
}
```

### Job with Minimum Interval (Using the Job Base Class)

```csharp
using Microsoft.Extensions.Logging;
using Shiny.Jobs;

public class PeriodicCleanupJob : Job
{
    readonly IMyLocalDb db;

    public PeriodicCleanupJob(ILogger<PeriodicCleanupJob> logger, IMyLocalDb db) : base(logger)
    {
        this.db = db;
        this.MinimumTime = TimeSpan.FromHours(6);
    }

    protected override Task RunJob(CancellationToken cancelToken)
        => this.db.PurgeOldRecords(cancelToken);
}
```

### Registering Jobs at Startup

```csharp
builder.Services.AddJob<DataSyncJob>(r => r
    .WithForeground()
    .WithInternet(InternetAccess.Any)
);

builder.Services.AddJob<PeriodicCleanupJob>(r => r
    .WithBatteryNotLow()
);
```

### Runtime Querying and On-Demand Execution

```csharp
public class JobDashboardViewModel(IJobManager jobManager)
{
    public async Task Initialize()
    {
        // List all registered jobs (keyed by Type)
        var jobs = jobManager.GetJobs();
        foreach (var (type, reg) in jobs)
            Console.WriteLine($"{type.FullName} foreground={reg.RunOnForeground}");

        // Run a specific job now (runs normally / inline)
        var result = await jobManager.RunJob(typeof(DataSyncJob));
        if (!result.Success)
        {
            // result.Exception holds the cause
        }

        // Force-run every registered job (concurrent by default)
        var results = await jobManager.RunAll(runSequentially: true);
        foreach (var r in results)
        {
            var name = r.Job?.JobType.FullName ?? "(unknown)";
            Console.WriteLine(r.Success ? $"{name} succeeded" : $"{name} failed: {r.Exception?.Message}");
        }
    }
}
```

### Checking Platform Access

```csharp
// IJobManager doesn't expose RequestAccess directly. Resolve the AbstractJobManager to call it.
public class StartupCheck(IJobManager jobManager)
{
    public async Task<bool> EnsureAvailable()
    {
        if (jobManager is not AbstractJobManager mgr)
            return false;

        var access = await mgr.RequestAccess();
        return access == AccessState.Available;
    }
}
```

### Running a Registered Job On Demand

```csharp
// Force a single registered job to run now. It runs normally (inline).
var result = await jobManager.RunJob(typeof(MyJob));
```

---

## Troubleshooting

### Jobs Not Running on iOS

- Ensure all four BGTask identifiers are registered in `Info.plist` under `BGTaskSchedulerPermittedIdentifiers`:
  - `com.shiny.job`
  - `com.shiny.jobpower`
  - `com.shiny.jobnet`
  - `com.shiny.jobpowernet`
- Ensure the `processing` background mode is enabled in `Info.plist`.
- `RequestAccess()` returns `AccessState.NotSetup` when configuration is missing or scheduler registration fails.
- Jobs do not run on the iOS Simulator.

### Jobs Not Running on Android

- AndroidX `WorkManager` has a minimum periodic interval of **15 minutes**.
- Ensure the device is not in battery optimization / Doze mode during testing.

### `RequestAccess()` Returns Unexpected State

| Return Value               | Meaning                                                      |
|----------------------------|--------------------------------------------------------------|
| `AccessState.Available`    | Everything is configured correctly                           |
| `AccessState.NotSetup`     | iOS plist configuration is missing or registration failed    |
| `AccessState.NotSupported` | Platform or device does not support background processing    |
| `AccessState.Disabled`     | User has disabled background processing for the app          |
| `AccessState.Restricted`   | System policy restricts background work                      |
| `AccessState.Denied`       | User denied the required permission                          |

### Job Constructor Injection Not Working

Jobs are resolved through the DI container per run. Make sure the services your job depends on are registered before `IJobManager` first runs the job.

### Foreground Jobs Not Firing

- Ensure the registration has `WithForeground()` applied.
- The foreground driver is `JobLifecycleTask` (platform builds only); its interval is `JobLifecycleTask.Interval` (default 1 minute, range 15s–5min).
- Foreground runs respect constraints: `RequiredInternetAccess`, `DeviceCharging`, and `BatteryNotLow` are checked before each run.
- On the base .NET TFM the in-process `JobManager` uses its own timer (`JobManager.Interval`, default 30s) instead of `JobLifecycleTask`.
