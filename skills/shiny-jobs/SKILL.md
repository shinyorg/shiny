---
name: shiny-jobs
description: Background job scheduling and execution for .NET MAUI (iOS/Android native OS schedulers) and in-process jobs for plain .NET, Linux, macOS, and Blazor WASM using Shiny.Jobs
auto_invoke: true
triggers:
  - background job
  - job manager
  - scheduled job
  - periodic job
  - background task
  - job registration
  - IJobManager
  - IJob
  - JobInfo
  - Shiny.Jobs
  - foreground job
  - job constraints
  - job scheduling
---

# Shiny Jobs

Shiny.Jobs provides cross-platform background job scheduling and execution. On iOS and Android it uses the native OS schedulers (BGTaskScheduler / AndroidX WorkManager). On plain .NET targets (Linux, macOS server, Blazor WASM, console, etc.) it runs an in-process managed JobManager driven by a recurring timer — jobs run only while the host process is alive; there is no OS-level scheduler on those targets. All platforms support the same constraints (network availability, charging, battery).

> **Blazor WASM caveat**: background jobs only run while the tab is open and foregrounded. Service Worker / Periodic Background Sync cannot invoke C# because the SW has no access to the Blazor WASM runtime. For true background HTTP work on Blazor, use `Shiny.Net.Http.Blazor` (which uses Service Worker Background Sync in pure JS and reconciles results to C# when the tab reopens).

## When to Use This Skill

- The user wants to schedule background work that runs periodically
- The user needs to run tasks when the app is not in the foreground
- The user asks about job constraints (network, charging, battery)
- The user wants to register, cancel, or manage background jobs
- The user needs foreground job execution on a timer
- The user asks about `IJobManager`, `IJob`, `JobInfo`, or related types

## Library Overview

| Item       | Value                                                                       |
|------------|-----------------------------------------------------------------------------|
| NuGet      | `Shiny.Jobs`                                                                |
| Namespace  | `Shiny.Jobs`                                                                |
| Platforms  | iOS, Android (native OS); Linux, macOS, Blazor WASM, .NET base (in-process) |

## Setup

Register jobs during service configuration in `MauiProgram.cs`:

```csharp
using Shiny;
using Shiny.Jobs;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // Register the job infrastructure and a specific job
        builder.Services.AddJob(
            typeof(MySyncJob),
            identifier: "MySync",
            requiredNetwork: InternetAccess.Any,
            runInForeground: true
        );

        // Or register with a full JobInfo record
        builder.Services.AddJob(new JobInfo(
            Identifier: "MyDataSync",
            JobType: typeof(MySyncJob),
            RunOnForeground: true,
            RequiredInternetAccess: InternetAccess.Any,
            DeviceCharging: false,
            BatteryNotLow: true
        ));

        return builder.Build();
    }
}
```

### iOS Setup

Add the following background task identifiers to your `Info.plist` under `BGTaskSchedulerPermittedIdentifiers`:

- `com.shiny.job`
- `com.shiny.jobpower`
- `com.shiny.jobnet`
- `com.shiny.jobpowernet`

Also enable the `processing` background mode.

### Android Setup

No additional manifest setup is required. Shiny.Jobs uses AndroidX WorkManager under the hood. If you want wake-lock support for `RunTask`, add the `WAKE_LOCK` permission to your `AndroidManifest.xml`.

### Plain .NET Setup (Linux, macOS, Blazor WASM, Console)

On the base .NET TFM there is no native OS scheduler — Shiny runs an in-process managed `JobManager` on a recurring timer (default 30s; configurable via the static `JobManager.Interval` property, minimum 15s, maximum 5 minutes). Jobs only execute while the host process is alive.

There is **no separate `Shiny.Jobs.Blazor` package** — reference `Shiny.Jobs` directly on all plain .NET targets (Blazor WASM included). You must register an `IBattery` and `IConnectivity` implementation before resolving the job manager; `AddJobs()` (called by `AddJob`) auto-registers the default JSON filesystem repository, `IKeyValueStoreFactory`, and `IObjectStoreBinder` so the `JobManager` can be constructed without pulling in a platform infrastructure layer.

```csharp
using Shiny;
using Shiny.Jobs;

// Linux / console — battery + connectivity come from Shiny.Core.Linux
services.AddConnectivity();
services.AddBattery();

// Blazor WASM — from Shiny.Core.Blazor
// services.AddConnectivity();
// services.AddBattery();
// Optionally override the default filesystem repo with browser localStorage:
// services.AddLocalStorageRepository();  // Shiny.Support.Storage.Blazor

services.AddJob(
    typeof(MySyncJob),
    identifier: "MySync",
    runInForeground: true,
    requiredNetwork: InternetAccess.Any
);
```

Jobs registered via `AddJob(...)` on the base TFM are treated as **system jobs**: on startup the manager clears any previously-registered system jobs, then re-registers them from the static in-memory list. This mirrors the iOS/Android behavior where system jobs survive across process restarts.

**Blazor WASM caveat**: the in-process JobManager only runs while the tab is open. Background tabs are throttled (~1 min timer floor on Chromium), may be frozen after ~5 minutes, and iOS Safari kills background WASM aggressively. There is no way to run C# jobs via Service Worker Background Sync because the SW has no access to the WASM runtime. For background HTTP work specifically, use `Shiny.Net.Http.Blazor`.

## Code Generation Instructions

When generating job-related code, follow these conventions:

1. **Implement `IJob` directly** for simple jobs, or **inherit from `Job`** (abstract base class) for jobs that need minimum time tracking between runs.
2. **Always provide a unique `Identifier`** for each `JobInfo` registration.
3. **Use constructor injection** -- job classes are resolved via DI, so inject any services you need through the constructor.
4. **Respect cancellation tokens** -- always pass and honor the `CancellationToken` provided to `Run`.
5. **Use `RunOnForeground = true`** when the job should also execute while the app is in the foreground on a timer (via `JobLifecycleTask`).
6. **Use `InternetAccess` constraints** to prevent jobs from running without the correct network state.
7. **Call `RequestAccess()`** before relying on background jobs to verify the platform supports and has granted background execution.
8. **Observe `JobStarted` and `JobFinished`** for monitoring or UI updates -- these are `IObservable<T>` streams.
9. **Register jobs at startup** using `services.AddJob(...)` so they are treated as system jobs and automatically re-registered on app restart.
10. **Do not call `CancelAll()` lightly** -- it cancels all non-system jobs. System jobs (registered at startup) are preserved.

### Conventions

- Job class names should end with `Job` (e.g., `DataSyncJob`, `CleanupJob`).
- Place job classes in a `Jobs/` folder or namespace within the project.
- Always handle exceptions inside `Run` gracefully; unhandled exceptions are logged but the job is marked as failed.
- Use `JobInfo.Parameters` dictionary for passing simple configuration data to jobs.
- `JobInfo` does NOT have a `LastRunUtc` property. Last run tracking is on the `Job` abstract base class (`LastRunTime` property), not on `JobInfo`.

## Best Practices

- **Check `RequestAccess()` on startup** and guide the user if background jobs are not available (`AccessState.NotSetup`, `AccessState.Disabled`, etc.).
- **Keep jobs short-lived** -- background execution time is limited by the OS, especially on iOS.
- **Use constraints wisely** -- setting `DeviceCharging = true` or `BatteryNotLow = true` means the job may not run for extended periods if conditions are not met.
- **Prefer `InternetAccess.Any` over `InternetAccess.Unmetered`** unless the job transfers large amounts of data.
- **Use the `Job` base class** when you need built-in minimum time spacing between runs via `MinimumTime`.
- **Monitor jobs** by subscribing to `IJobManager.JobStarted` and `IJobManager.JobFinished` for logging or UI feedback.
- **Use `RunJobAsTask`** extension method to bridge a registered job into a `Task` for event-driven scenarios.

## Reference Files

- [API Reference](reference/api-reference.md)
