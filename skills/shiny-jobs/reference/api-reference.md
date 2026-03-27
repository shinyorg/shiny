# Shiny.Jobs API Reference

## Installation

```xml
<PackageReference Include="Shiny.Jobs" />
```

Project dependencies (pulled in automatically):

- `Shiny.Core`
- `Shiny.Support.DeviceMonitoring`
- `Shiny.Support.Repositories`

Android additionally uses:

- `Xamarin.AndroidX.Work.Runtime`

## Namespace

```csharp
using Shiny.Jobs;
```

Registration extension methods are in the `Shiny` namespace:

```csharp
using Shiny;
```

---

## Interfaces

### IJob

The contract that all background jobs must implement.

```csharp
namespace Shiny.Jobs;

public interface IJob
{
    /// <summary>
    /// Runs your code
    /// </summary>
    /// <param name="jobInfo">The job info associated with this run</param>
    /// <param name="cancelToken">Cancellation token to observe</param>
    Task Run(JobInfo jobInfo, CancellationToken cancelToken);
}
```

### IJobManager

The primary service for managing and executing background jobs. Injected via DI as `IJobManager`.

```csharp
namespace Shiny.Jobs;

public interface IJobManager
{
    /// <summary>
    /// Runs a one time, adhoc task - on iOS, it will initiate a background task
    /// </summary>
    void RunTask(string taskName, Func<CancellationToken, Task> task);

    /// <summary>
    /// Flag to see if job manager is running registered tasks
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Fires just as a job is about to start
    /// </summary>
    IObservable<JobInfo> JobStarted { get; }

    /// <summary>
    /// Fires as each job finishes
    /// </summary>
    IObservable<JobRunResult> JobFinished { get; }

    /// <summary>
    /// Requests/ensures appropriate platform permissions where necessary
    /// </summary>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// This force runs the manager and any registered jobs
    /// </summary>
    /// <param name="cancelToken">Cancellation token</param>
    /// <param name="runSequentially">If true, jobs run one after another instead of concurrently</param>
    Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default, bool runSequentially = false);

    /// <summary>
    /// Run a specific job adhoc
    /// </summary>
    /// <param name="jobIdentifier">The unique identifier of the registered job</param>
    /// <param name="cancelToken">Cancellation token</param>
    Task<JobRunResult> Run(string jobIdentifier, CancellationToken cancelToken = default);

    /// <summary>
    /// Get a job by its registered name
    /// </summary>
    /// <param name="jobIdentifier">The unique identifier of the registered job</param>
    /// <returns>The JobInfo if found, null otherwise</returns>
    JobInfo? GetJob(string jobIdentifier);

    /// <summary>
    /// Gets current registered jobs
    /// </summary>
    IList<JobInfo> GetJobs();

    /// <summary>
    /// Create/register a new job
    /// </summary>
    /// <param name="jobInfo">The job info describing the job to register</param>
    void Register(JobInfo jobInfo);

    /// <summary>
    /// Cancel a specific job by name
    /// </summary>
    /// <param name="jobName">The unique identifier of the job to cancel</param>
    void Cancel(string jobName);

    /// <summary>
    /// Cancel all non-system jobs
    /// </summary>
    void CancelAll();
}
```

---

## Records

### JobInfo

The configuration record that describes a registered job. Implements `IRepositoryEntity` for persistence.

```csharp
namespace Shiny.Jobs;

public record JobInfo(
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    string Identifier,

    /// <summary>
    /// The Type of the IJob implementation class
    /// </summary>
    Type JobType,

    /// <summary>
    /// If true, the job will also run on a foreground timer while the app is active
    /// </summary>
    bool RunOnForeground = false,

    /// <summary>
    /// Optional key-value parameters passed to the job at runtime
    /// </summary>
    Dictionary<string, string>? Parameters = null,

    /// <summary>
    /// The required internet access level for the job to run
    /// </summary>
    InternetAccess RequiredInternetAccess = InternetAccess.None,

    /// <summary>
    /// If true, the job only runs when the device is charging
    /// </summary>
    bool DeviceCharging = false,

    /// <summary>
    /// If true, the job only runs when the battery is not low (above 20%)
    /// </summary>
    bool BatteryNotLow = false,

    /// <summary>
    /// If true, this is a system-managed job registered at startup. System jobs are not removed by CancelAll().
    /// </summary>
    bool IsSystemJob = false
) : IRepositoryEntity;
```

### JobRunResult

The result of a job execution.

```csharp
namespace Shiny.Jobs;

public record JobRunResult(
    /// <summary>
    /// The job info that was run, null if the job was not found
    /// </summary>
    JobInfo? Job,

    /// <summary>
    /// The exception if the job failed, null on success
    /// </summary>
    Exception? Exception
)
{
    /// <summary>
    /// True if the job completed without an exception
    /// </summary>
    public bool Success => this.Exception == null;
}
```

---

## Abstract Base Classes

### Job

An abstract base class for `IJob` that provides minimum time spacing between runs and persistent state tracking. Extends `NotifyPropertyChanged`.

```csharp
namespace Shiny.Jobs;

public abstract class Job : NotifyPropertyChanged, IJob
{
    protected ILogger Logger { get; }
    protected Job(ILogger logger);

    /// <summary>
    /// Implement this to provide your job logic
    /// </summary>
    protected abstract Task Run(CancellationToken cancelToken);

    /// <summary>
    /// The JobInfo associated with the current run
    /// </summary>
    protected JobInfo JobInfo { get; }

    /// <summary>
    /// Last runtime of this job. Null if never run before.
    /// This value persists between runs. It is not recommended to set this manually.
    /// </summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// Sets a minimum time between this job firing.
    /// CAREFUL: jobs tend to NOT run when the user's phone is not in use.
    /// This value persists between runs.
    /// </summary>
    public TimeSpan? MinimumTime { get; set; }
}
```

**Behavior:** When `MinimumTime` is set, the base class checks whether enough time has elapsed since `LastRunTime` before invoking `Run(CancellationToken)`. If the minimum interval has not elapsed, the job is skipped silently.

---

## Enums

### InternetAccess

Specifies the network requirement for a job.

```csharp
namespace Shiny.Jobs;

public enum InternetAccess
{
    /// <summary>No network requirement</summary>
    None = 0,

    /// <summary>Any internet connection (WiFi, cellular, etc.)</summary>
    Any = 1,

    /// <summary>Unmetered connection only (typically WiFi)</summary>
    Unmetered = 2
}
```

### JobState

Internal enum used for logging job lifecycle events.

```csharp
namespace Shiny.Jobs;

public enum JobState
{
    Start,
    Finish,
    Error
}
```

### AccessState

Defined in `Shiny` namespace (from Shiny.Core). Returned by `IJobManager.RequestAccess()`.

```csharp
namespace Shiny;

public enum AccessState
{
    /// <summary>Permission has not been checked or is in an unknown state</summary>
    Unknown,

    /// <summary>API is not supported on the current platform or device</summary>
    NotSupported,

    /// <summary>Platform manifest or plist configuration is missing</summary>
    NotSetup,

    /// <summary>The service has been disabled by the user</summary>
    Disabled,

    /// <summary>Permission granted in a limited fashion</summary>
    Restricted,

    /// <summary>The user denied permission</summary>
    Denied,

    /// <summary>All necessary permissions are granted</summary>
    Available
}
```

### NetworkAccess

```csharp
namespace Shiny.Jobs;

public enum NetworkAccess
{
    None,
    Unknown,
    Bluetooth,
    Ethernet,
    WiFi,
    Cellular
}
```

### NetworkReach

```csharp
namespace Shiny.Jobs;

public enum NetworkReach
{
    Unknown,
    None,
    Local,
    ConstrainedInternet,
    Internet
}
```

### PowerState

```csharp
namespace Shiny.Jobs;

public enum PowerState
{
    /// <summary>Power state hasn't been checked yet or is in an unknown state</summary>
    Unknown,

    /// <summary>Device is plugged in and charging</summary>
    Charging,

    /// <summary>Device is fully charged and plugged in</summary>
    Charged,

    /// <summary>No battery has been detected so device is plugged in</summary>
    NoBattery,

    /// <summary>Device is running on battery</summary>
    Discharging
}
```

---

## Extension Methods

### JobExtensions (on IJobManager)

```csharp
namespace Shiny.Jobs;

public static class JobExtensions
{
    /// <summary>
    /// Runs a registered job as a Task using RunTask internally.
    /// You don't need to use this if you are using foreground jobs (i.e., services.UseJobForegroundService).
    /// </summary>
    /// <param name="jobManager">The job manager instance</param>
    /// <param name="jobIdentifier">The unique identifier of the registered job</param>
    /// <returns>A task that completes when the job finishes</returns>
    public static Task RunJobAsTask(this IJobManager jobManager, string jobIdentifier);
}
```

### ServiceCollectionExtensions (on IServiceCollection)

Defined in the `Shiny` namespace. Available on platforms only.

```csharp
namespace Shiny;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register a job on the job manager using a full JobInfo record
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="jobInfo">The JobInfo describing the job</param>
    public static IServiceCollection AddJob(this IServiceCollection services, JobInfo jobInfo);

    /// <summary>
    /// Register a job on the job manager by type with optional configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="jobType">The Type of the IJob implementation</param>
    /// <param name="identifier">Optional unique identifier (defaults to jobType.FullName)</param>
    /// <param name="requiredNetwork">Network access constraint (default: None)</param>
    /// <param name="runInForeground">If true, also runs on foreground timer (default: false)</param>
    /// <param name="parameters">Optional key-value parameters</param>
    public static IServiceCollection AddJob(
        this IServiceCollection services,
        Type jobType,
        string? identifier = null,
        InternetAccess requiredNetwork = InternetAccess.None,
        bool runInForeground = false,
        params (string Key, string Value)[] parameters
    );

    /// <summary>
    /// Registers the job infrastructure (IJobManager, JobLifecycleTask, connectivity, battery).
    /// Called automatically by AddJob but can be called directly if you only want the infrastructure
    /// without registering a specific job at startup.
    /// </summary>
    public static IServiceCollection AddJobs(this IServiceCollection services);
}
```

---

## Usage Examples

### Basic Job Implementation

```csharp
using Shiny.Jobs;

public class DataSyncJob : IJob
{
    readonly IMyApiService api;
    readonly IMyLocalDb db;

    public DataSyncJob(IMyApiService api, IMyLocalDb db)
    {
        this.api = api;
        this.db = db;
    }

    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        var pending = await this.db.GetPendingUploads();
        foreach (var item in pending)
        {
            cancelToken.ThrowIfCancellationRequested();
            await this.api.Upload(item);
            await this.db.MarkUploaded(item.Id);
        }
    }
}
```

### Job with Minimum Time Spacing (Using Job Base Class)

```csharp
using Microsoft.Extensions.Logging;
using Shiny.Jobs;

public class PeriodicCleanupJob : Job
{
    readonly IMyLocalDb db;

    public PeriodicCleanupJob(ILogger<PeriodicCleanupJob> logger, IMyLocalDb db)
        : base(logger)
    {
        this.db = db;
        this.MinimumTime = TimeSpan.FromHours(6);
    }

    protected override async Task Run(CancellationToken cancelToken)
    {
        await this.db.PurgeOldRecords(cancelToken);
    }
}
```

### Registering Jobs at Startup

```csharp
// In MauiProgram.cs
builder.Services.AddJob(
    typeof(DataSyncJob),
    identifier: "DataSync",
    requiredNetwork: InternetAccess.Any,
    runInForeground: true
);

builder.Services.AddJob(new JobInfo(
    Identifier: "PeriodicCleanup",
    JobType: typeof(PeriodicCleanupJob),
    RunOnForeground: false,
    RequiredInternetAccess: InternetAccess.None,
    BatteryNotLow: true
));
```

### Runtime Job Management

```csharp
public class JobDashboardViewModel
{
    readonly IJobManager jobManager;

    public JobDashboardViewModel(IJobManager jobManager)
    {
        this.jobManager = jobManager;
    }

    public async Task Initialize()
    {
        // Check access
        var access = await this.jobManager.RequestAccess();
        if (access != AccessState.Available)
        {
            // Handle: NotSupported, NotSetup, Disabled, Denied, etc.
            return;
        }

        // List all registered jobs
        var jobs = this.jobManager.GetJobs();

        // Get a specific job
        var syncJob = this.jobManager.GetJob("DataSync");

        // Run a specific job on demand
        var result = await this.jobManager.Run("DataSync");
        if (!result.Success)
        {
            // Handle result.Exception
        }

        // Run all jobs
        var results = await this.jobManager.RunAll(runSequentially: true);

        // Cancel a specific job
        this.jobManager.Cancel("DataSync");

        // Cancel all non-system jobs
        this.jobManager.CancelAll();
    }
}
```

### Observing Job Events

```csharp
// Subscribe to job lifecycle events
jobManager.JobStarted.Subscribe(jobInfo =>
{
    Console.WriteLine($"Job '{jobInfo.Identifier}' started");
});

jobManager.JobFinished.Subscribe(result =>
{
    if (result.Success)
        Console.WriteLine($"Job '{result.Job?.Identifier}' completed successfully");
    else
        Console.WriteLine($"Job '{result.Job?.Identifier}' failed: {result.Exception?.Message}");
});
```

### Running a Job as an Adhoc Task

```csharp
// Fire a one-time task (uses background task APIs on each platform)
jobManager.RunTask("QuickUpload", async cancelToken =>
{
    await myService.UploadPendingData(cancelToken);
});

// Bridge a registered job into a Task (useful for event-driven scenarios)
await jobManager.RunJobAsTask("DataSync");
```

### Using Job Parameters

```csharp
// Register with parameters
builder.Services.AddJob(
    typeof(RegionSyncJob),
    identifier: "RegionSync",
    requiredNetwork: InternetAccess.Any,
    parameters: ("Region", "US"), ("BatchSize", "100")
);

// Access parameters inside the job
public class RegionSyncJob : IJob
{
    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        var region = jobInfo.Parameters?["Region"] ?? "Default";
        var batchSize = int.Parse(jobInfo.Parameters?["BatchSize"] ?? "50");
        // Use region and batchSize...
    }
}
```

### Registering a Job at Runtime

```csharp
// Register a new job dynamically (not at startup)
jobManager.Register(new JobInfo(
    Identifier: "DynamicExport",
    JobType: typeof(ExportJob),
    RunOnForeground: false,
    RequiredInternetAccess: InternetAccess.Unmetered,
    DeviceCharging: true,
    BatteryNotLow: true
));
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
- `RequestAccess()` returns `AccessState.NotSetup` when configuration is missing.
- Jobs do not run on the iOS Simulator.

### Jobs Not Running on Android

- AndroidX WorkManager has a minimum periodic interval of **15 minutes**.
- Ensure the device is not in battery optimization / Doze mode during testing.
- Add `WAKE_LOCK` permission if you need `RunTask` to use wake locks.

### `RequestAccess()` Returns Unexpected State

| Return Value            | Meaning                                                      |
|-------------------------|--------------------------------------------------------------|
| `AccessState.Available` | Everything is configured correctly                           |
| `AccessState.NotSetup`  | iOS plist configuration is missing or registration failed    |
| `AccessState.NotSupported` | Platform or device does not support background processing |
| `AccessState.Disabled`  | User has disabled background processing for the app          |
| `AccessState.Denied`    | User denied the required permission                          |

### `CancelAll()` Does Not Remove My Startup Jobs

`CancelAll()` only removes non-system jobs. Jobs registered at startup via `services.AddJob(...)` are marked as `IsSystemJob = true` and are preserved. To remove system jobs, cancel them individually with `Cancel(identifier)`.

### Job Constructor Injection Not Working

Job instances are resolved via `ActivatorUtilities.GetServiceOrCreateInstance`. Make sure the services your job depends on are registered in the DI container before the job runs.

### Foreground Jobs Not Firing

- Ensure `RunOnForeground = true` is set on the `JobInfo`.
- Foreground jobs are managed by `JobLifecycleTask` with a default interval of **30 seconds** (configurable between 15 seconds and 5 minutes).
- Foreground jobs respect constraints: `RequiredInternetAccess`, `DeviceCharging`, and `BatteryNotLow` are all checked before each foreground run.
