---
name: shiny-core
description: Core infrastructure, hosting, DI, key-value stores, lifecycle hooks, and platform abstractions for Shiny on .NET MAUI, iOS, Android, Mac Catalyst, macOS, Windows, Linux, and Blazor WebAssembly
auto_invoke: true
triggers:
  - shiny core
  - shiny setup
  - shiny host
  - HostBuilder
  - IHost
  - IPlatform
  - IKeyValueStore
  - IKeyValueStoreFactory
  - StoreKeys
  - AccessState
  - IShinyStartupTask
  - ShinyLifecycleTask
  - IAndroidLifecycle
  - IIosLifecycle
  - IMacLifecycle
  - UseShiny
  - AddShinyStores
  - AddGeneratedServices
  - Bind attribute
  - Service attribute
  - Singleton attribute
  - Scoped attribute
  - Transient attribute
  - NotifyPropertyChanged
  - key value store
  - settings store
  - secure store
  - platform abstraction
  - lifecycle hook
  - startup task
  - connectivity
  - IConnectivity
  - IBattery
  - AddConnectivity
  - AddBattery
  - IRepository
  - IRepositoryEntity
  - Shiny.Core
  - Shiny.Core.Linux
  - Shiny.Core.Blazor
  - Shiny.Hosting.Maui
  - Shiny.Hosting.Native
  - Shiny.Extensions.DependencyInjection
  - Shiny.Extensions.Stores
  - Shiny.Extensions.Configuration
---

# Shiny Core

Shiny.Core is the foundational library for the Shiny ecosystem. It provides the hosting model, platform abstractions, lifecycle hooks, connectivity/battery monitoring, and the AOT-friendly type registry that all other Shiny modules build upon. Storage and DI registration are layered on via the `Shiny.Extensions.DependencyInjection` and `Shiny.Extensions.Stores` source-generated packages — these are pulled in transitively by `Shiny.Core` so you don't add them manually.

## When to Use This Skill

- The user needs to set up Shiny hosting in a MAUI, native, Blazor, or Linux/macOS app
- The user asks about `IHost`, `HostBuilder`, or `UseShiny`
- The user needs key-value storage (`IKeyValueStore`, settings, secure store) via `Shiny.Extensions.Stores`
- The user wants source-generated persistence with `[Bind]` partial properties or a service-attributed DI registration with `[Service]` / `[Singleton]` / `[Scoped]` / `[Transient]`
- The user asks about platform abstractions (`IPlatform`, directories, main thread invocation)
- The user needs Android, iOS, or macOS lifecycle hooks (`IAndroidLifecycle`, `IIosLifecycle`, `IMacLifecycle`)
- The user needs startup tasks (`IShinyStartupTask`, `ShinyLifecycleTask`)
- The user asks about `AccessState`, permission handling, or `PermissionException`
- The user needs network connectivity monitoring (`IConnectivity`) or battery status (`IBattery`)
- The user needs an entity repository (`IRepository`, `IRepositoryEntity`) — provided by `Shiny.Extensions.Stores`
- The user asks about remote configuration (`IRemoteConfigurationProvider`) or `Shiny.Extensions.Configuration`
- The user needs observable collections (`INotifyReadOnlyCollection<T>`, `INotifyCollectionChanged<T>`, `BindingList<T>`)

## Library Overview

| Item       | Value                           |
|------------|---------------------------------|
| NuGet      | `Shiny.Core` (pulls in `Shiny.Extensions.DependencyInjection` + `Shiny.Extensions.Stores`) |
| Namespace  | `Shiny`, `Shiny.Hosting`, `Shiny.Net`, `Shiny.Power`, `Shiny.Collections`, `Shiny.Extensions.Stores` (storage), `Shiny.Extensions.Configuration` (remote config) |
| Platforms  | iOS, Mac Catalyst, macOS, Android, Windows, Linux, Blazor WebAssembly, plain .NET |

### Companion Libraries

| NuGet | Namespace | Purpose |
|-------|-----------|---------|
| `Shiny.Hosting.Maui` | `Shiny` | MAUI hosting integration (`UseShiny`) |
| `Shiny.Hosting.Native` | `Shiny` | Native hosting base classes (`ShinyAppDelegate`, `ShinyAndroidApplication`, `ShinyAndroidActivity`) |
| `Shiny.Core.Linux` | `Shiny` | Linux platform implementation + `AddConnectivity()` / `AddBattery()` |
| `Shiny.Core.Blazor` | `Shiny` | Blazor WebAssembly platform implementation + `AddConnectivity()` / `AddBattery()` |
| `Shiny.Extensions.DependencyInjection` | `Shiny` | Source-generated `[Service]` / `[Singleton]` / `[Scoped]` / `[Transient]` DI registration. Pulled in by `Shiny.Core`. |
| `Shiny.Extensions.Stores` | `Shiny.Extensions.Stores` | `IKeyValueStore`, `IRepository`, source-generated `[Bind]` partial-property persistence, static `Shiny.Stores.Default/Secure` accessor. Pulled in by `Shiny.Core`. |
| `Shiny.Extensions.Stores.Web` | `Shiny.Extensions.Stores` | Blazor WebAssembly `localStorage` / `sessionStorage` adapters (`AddShinyWebAssemblyStores()`) |
| `Shiny.Extensions.Serialization` | `Shiny.Extensions.Serialization` | AOT-safe System.Text.Json serializer extensions used by Shiny modules |
| `Shiny.Extensions.Configuration` | `Shiny.Extensions.Configuration` | Remote configuration provider and platform preferences |

## Setup

### MAUI Setup

In `MauiProgram.cs`, call `UseShiny()` on the `MauiAppBuilder`. This registers all core infrastructure services, the platform key/value stores, and lifecycle wiring automatically:

```csharp
using Shiny;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny(); // Registers Shiny core services, stores, and lifecycle hooks

        // Register your own services from [Service]/[Singleton]/[Scoped]/[Transient] attributes
        builder.Services.AddGeneratedServices();

        // Add device monitoring (these are no-ops if already registered)
        builder.Services.AddConnectivity();
        builder.Services.AddBattery();

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

### Linux / macOS / plain .NET Setup

`Shiny.Core.Linux` provides the Linux `IPlatform`, `IConnectivity`, and `IBattery` implementations and is targeted at console / GTK apps. Use the same `HostBuilder.Create()` flow and call `AddConnectivity()` / `AddBattery()` from `Shiny.Core.Linux` if you need device monitoring.

### Blazor WebAssembly Setup

For Blazor WASM, reference `Shiny.Core.Blazor` and call `AddConnectivity()` / `AddBattery()` to wire navigator-based monitoring. Storage requires `Shiny.Extensions.Stores.Web` and a call to `host.Services.UseShinyStores()` after `Build()` so the static `Shiny.Stores` accessor snapshots the `IJSRuntime`-backed `LocalStorageKeyValueStore`.

## Code Generation Instructions

When generating code that uses Shiny.Core, follow these conventions:

1. **Always call `UseShiny()`** in MAUI apps or inherit the proper native base classes. This is required before any Shiny module works.
2. **Register your own services via attributes from `Shiny.Extensions.DependencyInjection`** — `[Service(ServiceLifetime.Singleton)]`, or the shortcuts `[Singleton]`, `[Scoped]`, `[Transient]`. The source generator emits `services.AddGeneratedServices()` — call it once during host build. Multiple interfaces, keyed services, and open generics are honoured automatically.
3. **Use the `[Bind]` partial-property pattern from `Shiny.Extensions.Stores`** for persisted settings instead of an `INotifyPropertyChanged` base class. The generator emits getters/setters that round-trip through the store with zero reflection — fully AOT/trim safe.
4. **Implement `IShinyStartupTask`** for code that should run immediately after the DI container is built. Register it with `services.AddSingleton<IShinyStartupTask, MyTask>()` or tag it `[Singleton]` and explicitly add the `IShinyStartupTask` interface.
5. **Inherit `ShinyLifecycleTask`** for startup tasks that also need foreground/background application-lifecycle events. It composes `IAndroidLifecycle.IApplicationLifecycle`, `IIosLifecycle.IApplicationLifecycle`, `IMacLifecycle.IApplicationLifecycle`, and `IShinyStartupTask` into one base class.
6. **Implement `IAndroidLifecycle.*`, `IIosLifecycle.*`, or `IMacLifecycle.*` sub-interfaces** for platform-specific lifecycle hooks. Register them in DI and the lifecycle executor dispatches to them automatically.
7. **Inject a keyed `IKeyValueStore` from `Shiny.Extensions.Stores`** via `[FromKeyedServices(StoreKeys.Default)] IKeyValueStore store` (or `StoreKeys.Secure`). The store factory is also available via `IKeyValueStoreFactory.Get(alias)`.
8. **Use the static `Shiny.Stores.Default` / `Shiny.Stores.Secure` accessors** for one-off reads/writes outside DI contexts — the accessor self-bootstraps on first use after `AddShinyStores()` has run (call `host.Services.UseShinyStores()` after `Build()` on Blazor WASM).
9. **Use `IPlatform`** to access `AppData`, `Cache`, `Public` directories and `InvokeOnMainThread()`.
10. **Use `AccessState` enum** and `state.Assert()` extension method to validate permissions before proceeding with platform operations.
11. **Use the `Changed` C# events on `IConnectivity` and `IBattery`** to react to network or battery state — these are no longer observable. Rx has been removed from `Shiny.Core` and `Shiny.Jobs`; only `Shiny.BluetoothLE` retains reactive streams. Subscribe with `+= handler` and unsubscribe in your `Dispose` / page-leave hook.
12. **Use `IRepository`** for entity persistence, with entities implementing `IRepositoryEntity` (must have an `Identifier` property). The default implementation is a filesystem JSON store registered by `services.AddDefaultRepository()` and used internally by Locations, Notifications, and HTTP Transfers.
13. **For Blazor WASM**, call `host.Services.UseShinyStores()` immediately after `builder.Build()` so the static `Shiny.Stores` accessor captures the DI-resolved `LocalStorageKeyValueStore` (it needs `IJSRuntime`).

### Conventions

- Shiny services are typically singletons; the `[Singleton]` attribute is the right default.
- Persisted settings classes should be `partial class` with `[Bind]` partial properties from `Shiny.Extensions.Stores`.
- Place startup tasks in a `Tasks/` or `Infrastructure/` folder.
- Place settings classes in a `Settings/` or `Models/` folder.
- Always handle `AccessState.Denied`, `AccessState.Disabled`, and `AccessState.NotSetup` gracefully.
- Prefer C# events on Shiny.Core abstractions (`IConnectivity.Changed`, `IBattery.Changed`) over Rx — Rx is intentionally absent from Core.
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
global using Shiny.Extensions.Stores;   // IKeyValueStore, StoreKeys, [Bind]
global using Shiny.Jobs;
global using Shiny.Locations;
global using Shiny.BluetoothLE;
// Do NOT globally use: Shiny.Net, Shiny.Power, Shiny.Notifications, Shiny.Push, Shiny.BluetoothLE.Hosting
```

## Best Practices

- **Initialize Shiny early** -- `UseShiny()` must be called in the builder chain before building the MAUI app. For native apps, the host must be created and `Run()` called in the application startup.
- **Prefer attribute-based registration** -- tag services with `[Singleton]`/`[Scoped]`/`[Transient]` and let the `Shiny.Extensions.DependencyInjection` source generator emit `AddGeneratedServices()`. AOT-clean, no reflection at startup, multiple interfaces handled.
- **Prefer `[Bind]` partial properties** for persisted settings instead of `INotifyPropertyChanged` plumbing. The generator emits getters/setters that round-trip through the configured `IKeyValueStore`.
- **Keep startup tasks lightweight** -- `IShinyStartupTask.Start()` runs synchronously on the main thread at startup.
- **Use the right keyed store** -- `StoreKeys.Default` for general preferences (backed by SharedPreferences / NSUserDefaults / ApplicationData.LocalSettings / localStorage), `StoreKeys.Secure` for sensitive data (Android Keystore / iOS Keychain / Windows secure storage).
- **Use the static `Shiny.Stores.Default` / `Shiny.Stores.Secure` / `Shiny.Stores.Keyed(alias)` accessor** for one-off reads/writes outside DI.
- **Check `Host.IsInitialized`** before accessing `Host.Current` in code that may run before initialization.
- **Use `BindingList<T>`** for thread-safe observable collections that can be bound to UI.
- **Use the JSON contexts emitted by Shiny modules** if you mix `Shiny.Extensions.Serialization` with your own — Shiny.Jobs, Shiny.Locations, Shiny.Notifications, and Shiny.Net.Http each ship their own `JsonSerializerContext` for AOT safety.

## Reference Files

- [API Reference](reference/api-reference.md)
