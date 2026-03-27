---
name: shiny-core
description: Core infrastructure, hosting, DI, key-value stores, lifecycle hooks, and platform abstractions for Shiny on .NET MAUI, iOS, and Android
auto_invoke: true
triggers:
  - shiny core
  - shiny setup
  - shiny host
  - HostBuilder
  - IHost
  - IPlatform
  - IKeyValueStore
  - ISerializer
  - IObjectStoreBinder
  - IKeyValueStoreFactory
  - AccessState
  - IShinyStartupTask
  - IShinyComponentStartup
  - ShinyLifecycleTask
  - IAndroidLifecycle
  - IIosLifecycle
  - UseShiny
  - AddShinyService
  - NotifyPropertyChanged
  - ShinySubject
  - key value store
  - settings store
  - secure store
  - object store binder
  - platform abstraction
  - lifecycle hook
  - startup task
  - connectivity
  - IConnectivity
  - IBattery
  - IRepository
  - IRepositoryEntity
  - IRemoteConfigurationProvider
  - remote configuration
  - Shiny.Core
  - Shiny.Support.DeviceMonitoring
  - Shiny.Support.Repositories
  - Shiny.Extensions.Configuration
---

# Shiny Core

Shiny.Core is the foundational library for the Shiny ecosystem. It provides the hosting model, dependency injection infrastructure, key-value storage, object-store binding, platform abstractions, lifecycle hooks, and reactive helpers that all other Shiny modules build upon. Companion support libraries add connectivity monitoring, battery status, file-based repositories, and remote configuration.

## When to Use This Skill

- The user needs to set up Shiny hosting in a MAUI or native app
- The user asks about `IHost`, `HostBuilder`, or `UseShiny`
- The user needs key-value storage (`IKeyValueStore`, settings, secure store)
- The user wants to persist INotifyPropertyChanged objects automatically via `IObjectStoreBinder`
- The user asks about platform abstractions (`IPlatform`, directories, main thread invocation)
- The user needs Android or iOS lifecycle hooks (`IAndroidLifecycle`, `IIosLifecycle`)
- The user needs startup tasks (`IShinyStartupTask`, `IShinyComponentStartup`, `ShinyLifecycleTask`)
- The user asks about `AccessState`, permission handling, or `PermissionException`
- The user needs network connectivity monitoring (`IConnectivity`) or battery status (`IBattery`)
- The user needs a file-based entity repository (`IRepository`, `IRepositoryEntity`)
- The user asks about remote configuration (`IRemoteConfigurationProvider`)
- The user needs reactive helpers (`WhenAnyProperty`, `SubscribeAsync`, `SelectSwitch`, etc.)
- The user asks about observable collections (`INotifyReadOnlyCollection<T>`, `INotifyCollectionChanged<T>`, `BindingList<T>`)

## Library Overview

| Item       | Value                           |
|------------|---------------------------------|
| NuGet      | `Shiny.Core`                    |
| Namespace  | `Shiny`, `Shiny.Hosting`, `Shiny.Stores`, `Shiny.Net`, `Shiny.Power`, `Shiny.Collections` |
| Platforms  | iOS, Android, Mac Catalyst, Windows |

### Companion Libraries

| NuGet | Namespace | Purpose |
|-------|-----------|---------|
| `Shiny.Hosting.Maui` | `Shiny` | MAUI hosting integration (`UseShiny`) |
| `Shiny.Hosting.Native` | `Shiny` | Native hosting base classes (`ShinyAppDelegate`, `ShinyAndroidApplication`, `ShinyAndroidActivity`) |
| `Shiny.Support.DeviceMonitoring` | `Shiny.Net`, `Shiny.Power` | `IConnectivity` and `IBattery` |
| `Shiny.Support.Repositories` | `Shiny.Support.Repositories` | `IRepository` and `IRepositoryEntity` |
| `Shiny.Extensions.Configuration` | `Shiny.Extensions.Configuration` | Remote configuration and platform preferences |

## Setup

### MAUI Setup

In `MauiProgram.cs`, call `UseShiny()` on the `MauiAppBuilder`. This registers all core infrastructure services and lifecycle wiring automatically:

```csharp
using Shiny;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny(); // Registers Shiny core services and lifecycle hooks

        // Register your own services using AddShinyService for auto-binding
        builder.Services.AddShinyService<MySettings>();

        // Add device monitoring
        builder.Services.AddConnectivity();
        builder.Services.AddBattery();

        // Add repository
        builder.Services.AddDefaultRepository();

        return builder.Build();
    }
}
```

### Native (Non-MAUI) Setup

For native iOS apps, inherit from `ShinyAppDelegate`:

```csharp
[Register("AppDelegate")]
public class AppDelegate : ShinyAppDelegate
{
    protected override IHost CreateShinyHost()
    {
        var builder = HostBuilder.Create();
        // Register services on builder.Services
        return builder.Build();
    }
}
```

For native Android apps, inherit from `ShinyAndroidApplication` and use `ShinyAndroidActivity`:

```csharp
[Application]
public class MainApplication : ShinyAndroidApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) {}

    protected override IHost CreateShinyHost()
    {
        var builder = HostBuilder.Create();
        // Register services on builder.Services
        return builder.Build();
    }
}

[Activity(MainLauncher = true)]
public class MainActivity : ShinyAndroidActivity { }
```

## Code Generation Instructions

When generating code that uses Shiny.Core, follow these conventions:

1. **Always call `UseShiny()`** in MAUI apps or inherit the proper native base classes. This is required before any Shiny module works.
2. **Use `AddShinyService<T>()`** to register singleton services that implement interfaces. It auto-registers all interfaces, binds `INotifyPropertyChanged` objects to persistent storage, and calls `ComponentStart()` on `IShinyComponentStartup` implementations.
3. **Implement `IShinyStartupTask`** for code that should run immediately after the DI container is built. Register it with `services.AddSingleton<IShinyStartupTask, MyTask>()`.
4. **Inherit `ShinyLifecycleTask`** for startup tasks that also need foreground/background lifecycle events. Override `OnStateChanged(bool backgrounding)` and `Start()`.
5. **Implement `IAndroidLifecycle.*` or `IIosLifecycle.*` sub-interfaces** for platform-specific lifecycle hooks. Register them in DI and the lifecycle executor dispatches to them automatically.
6. **Use `IKeyValueStore`** for simple key-value persistence. Use `IKeyValueStoreFactory` to retrieve named stores (`"settings"`, `"secure"`, `"memory"`).
7. **Decorate classes with `[ObjectStoreBinder("storeAlias")]`** to control which store backs an `INotifyPropertyChanged` object.
8. **Use `NotifyPropertyChanged` base class** for view models and settings objects that should persist through the object store binder. It provides `Set<T>()` and `RaisePropertyChanged()`.
9. **Use `IPlatform`** to access `AppData`, `Cache`, `Public` directories and `InvokeOnMainThread()`.
10. **Use `AccessState` enum** and `state.Assert()` extension method to validate permissions before proceeding with platform operations.
11. **Use reactive extensions** (`WhenAnyProperty`, `SubscribeAsync`, `SelectSwitch`, `DisposedBy`) for composing observable streams.
12. **Use `IConnectivity.WhenChanged()`** and `IBattery.WhenChanged()`** for reactive monitoring of device state.
13. **Use `IRepository`** for entity persistence, with entities implementing `IRepositoryEntity` (must have an `Identifier` property).

### Conventions

- All Shiny services are singletons.
- Settings/state classes should inherit from `NotifyPropertyChanged`.
- Place startup tasks in a `Tasks/` or `Infrastructure/` folder.
- Place settings classes in a `Settings/` or `Models/` folder.
- Always handle `AccessState.Denied`, `AccessState.Disabled`, and `AccessState.NotSetup` gracefully.
- Use `System.Reactive` (`IObservable<T>`) for event streams, not C# events.
- Extension methods in `Shiny` namespace are available when the appropriate package is referenced.

## Namespace Ambiguities with MAUI

When using Shiny in a MAUI app, several Shiny types collide with MAUI implicit usings. **Do NOT add all Shiny namespaces as global usings.** Use explicit namespaces or FQNs for these:

| Type | Shiny Namespace | MAUI Namespace | Resolution |
|------|----------------|----------------|------------|
| `IConnectivity` | `Shiny.Net` | `Microsoft.Maui.Networking` | Use `Shiny.Net.IConnectivity` FQN |
| `IBattery` | `Shiny.Power` | `Microsoft.Maui.Devices` | Use `Shiny.Power.IBattery` FQN |
| `DeviceInfo` | `Shiny.BluetoothLE` | `Microsoft.Maui.Devices` | Use FQN for whichever you need |

**Safe global usings** (won't conflict with MAUI):
```csharp
global using Shiny;
global using Shiny.Stores;
global using Shiny.Jobs;
global using Shiny.Locations;
global using Shiny.BluetoothLE;
// Do NOT globally use: Shiny.Net, Shiny.Power, Shiny.Notifications, Shiny.Push, Shiny.BluetoothLE.Hosting
```

## Best Practices

- **Initialize Shiny early** -- `UseShiny()` must be called in the builder chain before building the MAUI app. For native apps, the host must be created and `Run()` called in the application startup.
- **Prefer `AddShinyService<T>`** over manual DI registration for types that implement interfaces or `INotifyPropertyChanged`. It handles all the wiring automatically.
- **Keep startup tasks lightweight** -- `IShinyStartupTask.Start()` runs synchronously on the main thread at startup.
- **Use named stores appropriately** -- `"settings"` for general preferences (backed by SharedPreferences/NSUserDefaults), `"secure"` for sensitive data (backed by Android Keystore/iOS Keychain).
- **Dispose observable subscriptions** -- use `DisposedBy(CompositeDisposable)` to manage subscription lifetimes.
- **Use `StoreExtensions.Get<T>()`** for type-safe retrieval with default values. Use `GetRequired<T>()` when a missing key should throw.
- **Use `SetOrRemove()`** to automatically clean up null/default values from the store.
- **Check `Host.IsInitialized`** before accessing `Host.Current` in code that may run before initialization.
- **Use `BindingList<T>`** for thread-safe observable collections that can be bound to UI.
- **Use `ShinySubject<T>`** instead of raw `Subject<T>` for error-safe multicasting with logging support.

## Reference Files

- [API Reference](reference/api-reference.md)
