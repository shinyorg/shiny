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
  - JobRegistration
  - Shiny.Jobs
  - foreground job
  - job constraints
  - job scheduling
---

# Shiny Jobs

Shiny.Jobs provides cross-platform background job scheduling and execution. On iOS and Android it uses the native OS schedulers (`BGTaskScheduler` / AndroidX `WorkManager`); on Windows it uses COM-activated background tasks. On plain .NET targets (Linux, macOS server, Blazor WASM, console, etc.) it runs an in-process managed `JobManager` driven by a recurring timer — jobs run only while the host process is alive; there is no OS-level scheduler on those targets. All platforms support the same constraints (network availability, charging, battery).

> **Blazor WASM caveat**: background jobs only run while the tab is open and foregrounded. Service Worker / Periodic Background Sync cannot invoke C# because the SW has no access to the Blazor WASM runtime. For true background HTTP work on Blazor, use `Shiny.Net.Http.Blazor`.

## When to Use This Skill

- The user wants to schedule background work that runs periodically
- The user needs to run tasks when the app is not in the foreground
- The user asks about job constraints (network, charging, battery)
- The user wants to register or manage background jobs
- The user needs foreground job execution on a timer
- The user asks about `IJobManager`, `IJob`, `JobRegistration`, or related types

## Library Overview

| Item       | Value                                                                                  |
|------------|----------------------------------------------------------------------------------------|
| NuGet      | `Shiny.Jobs`                                                                           |
| Namespace  | `Shiny.Jobs` (types); `Shiny` (registration extensions)                                |
| Platforms  | iOS, Android, Windows (native OS); Linux, macOS, Blazor WASM, .NET base (in-process)   |

## Setup

Register jobs during service configuration. Each job is registered by its CLR type using `AddJob<TJob>` with optional fluent configuration. The job's `Type` is its unique identity — there is no separate string identifier.

```csharp
using Shiny;
using Shiny.Jobs;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.Services.AddJob<MySyncJob>(r => r
            .WithForeground()
            .WithInternet(InternetAccess.Any)
            .WithBatteryNotLow()
        );

        return builder.Build();
    }
}
```

`AddJob<TJob>` registers the infrastructure on first call (job manager + lifecycle task + battery/connectivity dependencies), then adds your job. Subsequent calls only add jobs.

### iOS Setup

Add the following background task identifiers to your `Info.plist` under `BGTaskSchedulerPermittedIdentifiers`:

- `com.shiny.job`
- `com.shiny.jobpower`
- `com.shiny.jobnet`
- `com.shiny.jobpowernet`

Also enable the `processing` background mode.

### Android Setup

No additional manifest setup is required. Shiny.Jobs uses AndroidX `WorkManager` under the hood (minimum periodic interval: 15 minutes).

### Windows Setup

Windows uses COM-activated in-process background tasks. Call `ShinyJobsBackgroundTask.RegisterComServer()` once in your `App` constructor before any trigger registration, and declare a matching `windows.comServer` extension in your appx manifest. Note: no background-mode triggering exists today on Windows — tasks fire while the process is running.

### Plain .NET Setup (Linux, macOS, Blazor WASM, Console)

On the base .NET TFM there is no native OS scheduler — Shiny runs an in-process managed `JobManager` on a recurring timer (default 30s; configurable via the static `JobManager.Interval` property, minimum 15s, maximum 5 minutes). Jobs only execute while the host process is alive.

There is **no separate `Shiny.Jobs.Blazor` package** — reference `Shiny.Jobs` directly on all plain .NET targets (Blazor WASM included). You must register an `IBattery` and `IConnectivity` implementation; `AddJob<TJob>(...)` auto-registers them via `AddConnectivity()`/`AddBattery()` from the appropriate platform support package if present.

```csharp
using Shiny;
using Shiny.Jobs;

// Linux / console — battery + connectivity come from Shiny.Core.Linux (via AddConnectivity/AddBattery)
// Blazor WASM — from Shiny.Support.DeviceMonitoring.Blazor
services.AddJob<MySyncJob>(r => r
    .WithForeground()
    .WithInternet(InternetAccess.Any)
);
```

**Blazor WASM caveat**: the in-process `JobManager` only runs while the tab is open. Background tabs are throttled (~1 min timer floor on Chromium), may be frozen after ~5 minutes, and iOS Safari kills background WASM aggressively. There is no way to run C# jobs via Service Worker Background Sync because the SW has no access to the WASM runtime. For background HTTP work specifically, use `Shiny.Net.Http.Blazor`.

## Code Generation Instructions

When generating job-related code, follow these conventions:

1. **Implement `IJob` directly** for simple jobs, or **inherit from the `Job` abstract base class** for jobs that need a minimum interval between runs (`MinimumTime`).
2. **The job's CLR type is its identity** — do not invent a separate string identifier. Pass `typeof(TJob)` to `RunJob(...)`; iterate `GetJobs()` for the registered set.
3. **Use constructor injection** — job classes are resolved through DI. Inject dependencies through the constructor.
4. **`IJob.Run` takes only `CancellationToken`** — there is no `JobInfo` argument. If a job needs configuration, inject it through the constructor or read from a settings store.
5. **Honor the `CancellationToken`** — the platform may cancel a run on time-budget expiry; check `cancelToken.ThrowIfCancellationRequested()` between work units.
6. **Use `WithForeground()`** when the job should also execute periodically while the app is in the foreground (driven by `JobLifecycleTask` — platform builds only).
7. **Use `WithInternet(InternetAccess.Any | Unmetered)`** to require connectivity before the job runs.
8. **Use `WithCharging()` / `WithBatteryNotLow()`** for power-sensitive jobs — be aware these may delay a job indefinitely under hostile conditions.
9. **Call `RequestAccess()`** (on the concrete `JobManager` via `AbstractJobManager`) before relying on background execution, so you can guide the user if a platform configuration is missing.
10. **Register jobs at host build time** via `services.AddJob<TJob>(...)`. There is no public runtime-register API on `IJobManager`.

### Conventions

- Job class names should end with `Job` (e.g., `DataSyncJob`, `CleanupJob`).
- Place job classes in a `Jobs/` folder or namespace within the project.
- Always handle exceptions inside `Run` gracefully; unhandled exceptions are caught by the manager, logged, and surfaced via the returned `JobRunResult`.
- Job instances are resolved per run from the DI container — keep them scoped-safe (no long-lived per-job state outside DI).

## Best Practices

- **Check `RequestAccess()` on startup** and guide the user if background jobs are not available (`AccessState.NotSetup`, `AccessState.Restricted`, `AccessState.Denied`, etc.).
- **Keep jobs short-lived** — background execution time is limited by the OS, especially on iOS where `BGProcessingTask` budgets are short.
- **Use constraints wisely** — `DeviceCharging = true` or `BatteryNotLow = true` means the job may not run for extended periods under hostile conditions.
- **Prefer `InternetAccess.Any` over `Unmetered`** unless the job transfers large amounts of data.
- **Use the `Job` base class with `MinimumTime`** when the OS may invoke your job more often than necessary and you want a simple in-memory throttle.
- **Use `RunJob(typeof(TJob))`** for event-driven on-demand runs of a registered job. The job runs normally (inline); there is no extended-execution / background-task wrapping.

## Reference Files

- [API Reference](reference/api-reference.md)
