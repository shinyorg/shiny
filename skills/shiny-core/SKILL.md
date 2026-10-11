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
  - IAppStartupInstaller
  - AddAppStartupInstaller
  - AppStartupOptions
  - AppStartupState
  - LinuxStartupMode
  - IStartupService
  - run at startup
  - launch at login
  - login item
  - autostart
  - systemd unit
  - DeviceIdiom
  - CultureChanged
  - TimeZoneChanged
  - OpenAppSettings
  - DeviceManufacturer
  - DeviceModel
  - AppVersion
  - UserAgent
  - UserAgentVersion
  - IKeyValueStore
  - IKeyValueStoreFactory
  - StoreKeys
  - AccessState
  - ShinyPermissions
  - ShinyPermission
  - maui permission
  - Permissions.RequestAsync
  - Permissions.CheckStatusAsync
  - BasePermission
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
  - BatteryPowerSource
  - EnergySaverStatus
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
| Platforms  | iOS, tvOS, Mac Catalyst, macOS, Android, Windows, Linux, Blazor WebAssembly, plain .NET |

### tvOS

tvOS reuses the iOS platform layer wholesale — the same `ShinyAppDelegate` (from `Shiny.Hosting.Native`), `IosPlatform`, `IosLifecycleExecutor` and `IIosLifecycle.*` hooks. There is no tvOS-specific hosting code to write, and **there is no MAUI on tvOS**, so a tvOS head always hosts through `Shiny.Hosting.Native` — never generate `UseShiny()` / `MauiProgram.cs` guidance for tvOS.

Two Core APIs differ on tvOS:

- **`IIosLifecycle.INotificationHandler` does not exist on tvOS.** A tvOS notification can only change the app icon badge, so there is no `UNNotificationResponse` and nothing is presented in the foreground. Code implementing it must be inside `#if !TVOS`.
- **`IBattery` reports `BatteryState.Full` / `Level` 1.0 / `PowerSource` AC / `EnergySaverStatus` Off permanently.** An Apple TV is mains powered and `UIDevice` carries no battery API on tvOS. `IBattery.Changed` never fires there.

A complete UIKit tvOS host is in `samples/Sample.tvOS` — use its `AppDelegate` as the shape for tvOS hosting guidance.

Modules with a `net10.0-tvos` target: `Shiny.Core`, `Shiny.Hosting.Native`, `Shiny.BluetoothLE`, `Shiny.Net.Discovery`, `Shiny.Jobs`, `Shiny.Net.Http`, `Shiny.Push`, `Shiny.ScreenRecorder`, `Shiny.Data.Sync`. Modules with **no** tvOS target, because Apple withholds the underlying API: `Shiny.BluetoothLE.Hosting` (no peripheral role), `Shiny.Net.Wifi` (no NetworkExtension hotspot APIs), `Shiny.Gps` / `Shiny.Geofencing` / `Shiny.Locations` (no `CLMonitor` geofencing), `Shiny.Notifications`, `Shiny.Contacts`, `Shiny.Calendar`.

### Companion Libraries

| NuGet | Namespace | Purpose |
|-------|-----------|---------|
| `Shiny.Hosting.Maui` | `Shiny` | MAUI hosting integration (`UseShiny`) |
| `Shiny.Hosting.Native` | `Shiny` | Native hosting base classes (`ShinyAppDelegate`, `ShinyAndroidApplication`, `ShinyAndroidActivity`) |
| `Shiny.Extensions.DependencyInjection` | `Shiny` | Source-generated `[Service]` / `[Singleton]` / `[Scoped]` / `[Transient]` DI registration. Pulled in by `Shiny.Core`. |
| `Shiny.Extensions.Stores` | `Shiny.Extensions.Stores` | `IKeyValueStore`, `IRepository`, source-generated `[Bind]` partial-property persistence, static `Shiny.Stores.Default/Secure` accessor. Pulled in by `Shiny.Core`. |
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
        // Optional on platform TFMs: AddJob / AddHttpTransfers / AddDataSync add them automatically.
        builder.Services.AddConnectivity();
        builder.Services.AddBattery();

        return builder.Build();
    }
}
```

`UseShiny()` also covers macOS (AppKit) MAUI apps built on `Microsoft.Maui.Platforms.MacOS` - `Shiny.Hosting.Maui` ships a `net10.0-macos` target, so call it after `UseMauiAppMacOS<App>()` like any other head. Never hand-roll an `IMauiInitializeService` that news up `Shiny.Hosting.Host` or call `AddShinyCoreServices()` yourself - that is what `UseShiny()` does. (Doing both is harmless - `Host.Run()` ignores a second run over the same service provider and `AddShinyCoreServices()` is idempotent - but it is dead code.) The macOS MAUI backend has no lifecycle events for remote notifications, so a macOS head that **uses push** forwards `application:didRegisterForRemoteNotificationsWithDeviceToken:` and friends from its `MacOSMauiApplication` subclass to `Host.Lifecycle` (see `samples/Sample.MacOS/Main.cs`). Only add those methods when the app actually uses push - implementing them is what tells Apple the app uses push, and App Review flags apps that implement them without it. Shiny deliberately ships no base delegate that overrides them.

### MAUI Permission Checks (`ShinyPermissions`)

`Shiny.Hosting.Maui` ships `ShinyPermissions` - MAUI `Permissions.BasePermission` subclasses that ask the OS for exactly what each Shiny module needs, **without referencing the module**. Use them when you want MAUI's permission flow (a permissions screen, onboarding) instead of, or ahead of, the module's own `RequestAccess`.

```csharp
using Shiny;

// prefer the instance form - with `using Shiny;`, an unqualified `Permissions` is ambiguous on Windows
var status = await new ShinyPermissions.BluetoothLE().RequestAsync();
var current = await new ShinyPermissions.GpsBackground().CheckStatusAsync();

// the MAUI static form works too when written out
var push = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<ShinyPermissions.Push>();
```

| Check | Mirrors | Android | Apple | Windows |
|-------|---------|---------|-------|---------|
| `BluetoothLE` | `IBleManager.RequestAccess` | SCAN + CONNECT (12+) / BLUETOOTH + ADMIN + FINE (below), then adapter on | CoreBluetooth central authorization + power state | LE radio on |
| `BluetoothLEHosting` | `IBleHostingManager.RequestAccess` | ADVERTISE + CONNECT (12+), adapter | CoreBluetooth peripheral authorization + power | peripheral role supported, radio on |
| `Beacons` | ranging | = `BluetoothLE` | when-in-use location (ranging available) | = `BluetoothLE` |
| `BeaconMonitoring` | monitoring | BLE + connected-device FGS (14+) + POST_NOTIFICATIONS (13+) | "always" location (not macOS) | = `BluetoothLE` |
| `Gps` | `GpsRequest.Foreground` | coarse + fine | when-in-use | `location` capability |
| `GpsBackground` | `GpsBackgroundMode.Standard` | coarse + fine, then background location on its own (10+) | when-in-use, then "always" | not supported |
| `GpsRealtime` | `GpsBackgroundMode.Realtime` | coarse + fine + FGS + POST_NOTIFICATIONS + FGS location (bg location on 10/11) | "always" | not supported |
| `Geofencing` | `IGeofenceManager.RequestAccess` | coarse + fine, then background location | "always" | `location` capability |
| `MotionActivity` | `IMotionActivityManager` | ACTIVITY_RECOGNITION (10+) | CoreMotion (not macOS) | not supported |
| `Notifications` | `RequestAccess()` | POST_NOTIFICATIONS (13+) + app notifications on | alert/badge/sound | toast setting |
| `TimeSensitiveNotifications` | `AccessRequestFlags.TimeSensitivity` | + exact alarms (12+, opens Settings) | = `Notifications` | = `Notifications` |
| `Push` | user-facing half of push | POST_NOTIFICATIONS (13+) | alert/badge/sound | toast setting |
| `HttpTransfers`, `DataSync` | foreground service | FGS + POST_NOTIFICATIONS | granted | granted |
| `Jobs` | `IJobManager.RequestAccess` | granted | `processing` mode + BGTask identifiers declared, Background App Refresh | not supported |
| `Wifi` | `IWifiManager.RequestAccess` | fine (+ NEARBY_WIFI_DEVICES 13+) | when-in-use | `wiFiControl` |
| `Contacts` | `IContactStore.RequestAccess` | READ + WRITE_CONTACTS | CNContactStore (iOS 18 limited = `Limited`) | not supported |
| `Calendar` | `ICalendarStore.RequestAccess(ReadWrite)` | READ + WRITE_CALENDAR | full access (iOS 17 / macOS 14+) | appointments store |
| `ScreenRecorder` | without audio | FGS + POST_NOTIFICATIONS | ReplayKit available / macOS screen capture (relaunch to apply) | capture supported |

- **Results**: partial grants (approximate location, when-in-use for a background check, iOS 18 limited contacts, calendar write-only, notifications denied for a foreground service, no exact alarms) are `PermissionStatus.Limited`. iOS parental-control restrictions are `Restricted`. A switched-off service or radio is `Disabled`.
- **Throws**: `FeatureNotSupportedException` when the platform or device can't do the feature (and on the plain `net10.0` / Linux GTK build, which has no checks); `Microsoft.Maui.ApplicationModel.PermissionException` when the manifest, Info.plist or (packaged) Windows capability entry is missing.
- **Android needs the Shiny host running** - `UseShiny()` - because requests go through `AndroidPlatform`.
- Pair `ScreenRecorder` with MAUI's `Permissions.Microphone` for audio. LiveActivities (iOS needs the Swift bridge), GameCenter, Wearables and Network Discovery have no check here.

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

There are **no** `Shiny.Core.Linux` or `Shiny.Core.Blazor` packages any more - `Shiny.Core`'s plain `net10.0` build carries the Linux and Blazor WebAssembly implementations and `AddConnectivity()` / `AddBattery()` pick one at runtime with `OperatingSystem.IsBrowser()` / `OperatingSystem.IsLinux()`. On plain .NET, `IConnectivity` is the browser monitor on Blazor and `System.Net.NetworkInformation` everywhere else; `IBattery` is the browser Battery Status API on Blazor, sysfs on Linux, and **not registered** on plain .NET for Windows or macOS (register your own if a module such as `AddJob` needs one). A Linux GTK **MAUI** app (`Microsoft.Maui.Platforms.Linux.Gtk4`) uses `UseShiny()` from `Shiny.Hosting.Maui` like any other MAUI head - call it after `AddLinuxGtk4Essentials()`; it runs the host and points `NetPlatform.MainThreadHandler` at the GTK MAUI dispatcher. A console, service or non-MAUI GTK app uses the same `HostBuilder.Create()` flow. Modules that need device monitoring (`AddJob`, `AddHttpClientTransfers`, `AddDataSync`) add it themselves on every target.

### Blazor WebAssembly Setup

For Blazor WASM, reference `Shiny.Core` (its JS modules are served from `_content/Shiny.Core/`), call `builder.Services.AddShinyCoreServices()` (namespace `Shiny.Infrastructure`) to register `IPlatform` (`NetPlatform`) - nothing else does on plain `net10.0` - and call `AddConnectivity()` / `AddBattery()` to wire navigator-based monitoring. `NetPlatform.UserAgent` / `UserAgentVersion` give the browser user agent and a best-effort parsed browser version (null outside a browser); inject `NetPlatform` for them. These, plus `IPlatform.AppVersion` / `CultureChanged` / `TimeZoneChanged`, replace the members removed from `Shiny.Extensions.BlazorHosting`'s `IAppSupport`, which now only has screen/viewport sizes. Both monitors load a JS module before they can report anything, so they self-start on the first property read or `Changed` subscription and report `Unknown` until that completes. **Always** call `await host.Services.UseShiny()` after `Build()` and before `RunAsync()` - it runs the Shiny host (every `IShinyStartupTask`, including the in-process job runner, which otherwise never starts) and then starts the monitors up front. Only Chromium-based browsers expose the Network Information and Battery Status APIs — elsewhere `ConnectionTypes` and `BatteryState` stay `Unknown` (`Access` still works, it is `navigator.onLine`). Storage comes from `Shiny.Extensions.Stores` (its `net10.0` build includes the localStorage stores - `AddLocalStorageKeyValueStore()` / `AddLocalStorageRepository()`; `Shiny.Extensions.Stores.Web` no longer exists) and needs a call to `host.Services.UseShinyStores()` after `Build()` so the static `Shiny.Stores` accessor snapshots the `IJSRuntime`-backed `LocalStorageKeyValueStore`.

### Replacing IConnectivity / IBattery

Modules that depend on device monitoring add it for you on every target: `AddJob` adds both, `AddHttpTransfers` / `AddHttpClientTransfers` / `AddDataSync` add `IConnectivity`, and the Blazor `AddBlazorHttpTransfers` / `AddBlazorDataSync` add both. None of them replace an existing registration, so to substitute your own implementation register it **before** the module call:

```csharp
builder.Services.AddSingleton<Shiny.Net.IConnectivity, MyConnectivity>(); // first - wins
builder.Services.AddSingleton<Shiny.Power.IBattery, MyBattery>();
builder.Services.AddJob<MyJob>();                                          // TryAdd skips its defaults
```

Registering yours after the module call adds a second descriptor; `GetService` returns the last one, but anything enumerating `IEnumerable<IConnectivity>` sees both, so always register first.

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
9. **Use `IPlatform`** to access `AppData`, `Cache`, `Public` directories and `InvokeOnMainThread()`, and for device/app info: `AppVersion`, `DeviceManufacturer`, `DeviceModel`, `PlatformVersion`, `Platform` (`Android`, `iOS`, `MacCatalyst`, `tvOS`, `macOS`, `Windows`, `Linux`, `Browser`), `DeviceIdiom` (`Shiny.DeviceIdiom` - never MAUI's `Microsoft.Maui.Devices.DeviceIdiom`; qualify it when both namespaces are imported), `CurrentCulture` / `CultureChanged`, `CurrentTimeZone` / `TimeZoneChanged` (native listeners start with the first subscriber; raised off the UI thread - marshal before touching UI) and `OpenAppSettings()`. These are implemented natively with no MAUI, and replace the members removed from `Shiny.Extensions.MauiHosting`'s `IAppSupport` - never generate them against `IAppSupport`.
10. **Use `IAppStartupInstaller`** (register with `services.AddAppStartupInstaller(o => ...)`) to put the app in the OS launch-at-login list: `HKCU\...\Run` on unpackaged Windows, `SMAppService` login items on macOS 13+ / Mac Catalyst 16+, and on Linux an XDG autostart entry or a systemd unit chosen by `AppStartupOptions.LinuxMode` (`XdgAutostart`, `SystemdGraphicalSession`, `SystemdUser`, `SystemdSystem` - the last needs root). Check `IsSupported` before showing a toggle; `Register()` returns the state the OS settled on (`Enabled`, `DisabledByUser`, `DisabledByPolicy`, `RequiresApproval` on macOS) - bind UI to that or a fresh `GetState()`, never cache it, and call `OpenSettings()` when the user has to finish in the OS. Android/iOS/tvOS report `NotSupported`. It replaces `Shiny.Extensions.MauiHosting`'s `IStartupService` / `AddStartupService` - never generate those.
11. **Use `AccessState` enum** and `state.Assert()` extension method to validate permissions before proceeding with platform operations.
12. **Use the `Changed` C# events on `IConnectivity` and `IBattery`** to react to network or battery state — these are no longer observable. Rx has been removed from `Shiny.Core` and `Shiny.Jobs`; only `Shiny.BluetoothLE` retains reactive streams. Subscribe with `+= handler` and unsubscribe in your `Dispose` / page-leave hook.
13. **Use `IRepository`** for entity persistence, with entities implementing `IRepositoryEntity` (must have an `Identifier` property). The default implementation is a filesystem JSON store registered by `services.AddDefaultRepository()` and used internally by Locations, Notifications, and HTTP Transfers.
14. **For Blazor WASM**, call `host.Services.UseShinyStores()` immediately after `builder.Build()` so the static `Shiny.Stores` accessor captures the DI-resolved `LocalStorageKeyValueStore` (it needs `IJSRuntime`).

### Conventions

- Shiny services are typically singletons; the `[Singleton]` attribute is the right default.
- Persisted settings classes should be `partial class` with `[Bind]` partial properties from `Shiny.Extensions.Stores`.
- Place startup tasks in a `Tasks/` or `Infrastructure/` folder.
- Place settings classes in a `Settings/` or `Models/` folder.
- Always handle `AccessState.Denied`, `AccessState.Disabled`, and `AccessState.NotSetup` gracefully.
- `IBattery` covers what .NET MAUI Essentials' `IBattery` does — `PowerSource` and `EnergySaverStatus` alongside `Status` / `Level` — so prefer it over Essentials when you want battery state without a MAUI dependency (a Linux or console host, a library).
- Prefer C# events on Shiny.Core abstractions (`IConnectivity.Changed`, `IBattery.Changed`) over Rx — Rx is intentionally absent from Core.
- Extension methods in `Shiny` namespace are available when the appropriate package is referenced.

## Namespace Ambiguities with MAUI

When using Shiny in a MAUI app, several Shiny types collide with MAUI implicit usings. **Do NOT add all Shiny namespaces as global usings.** Use explicit namespaces or FQNs for these:

| Type | Shiny Namespace | MAUI Namespace | Resolution |
|------|----------------|----------------|------------|
| `IConnectivity` | `Shiny.Net` | `Microsoft.Maui.Networking` | Use `Shiny.Net.IConnectivity` FQN |
| `IBattery` | `Shiny.Power` | `Microsoft.Maui.Devices` | Use `Shiny.Power.IBattery` FQN |
| `BatteryState`, `BatteryPowerSource`, `EnergySaverStatus` | `Shiny.Power` | `Microsoft.Maui.Devices` | Use `Shiny.Power.*` FQN (in the global namespace in 5.7.1 only — see below) |
| `DeviceInfo` | `Shiny.BluetoothLE` | `Microsoft.Maui.Devices` | Use FQN for whichever you need |
| `PermissionException` | `Shiny` | `Microsoft.Maui.ApplicationModel` | `ShinyPermissions` checks throw the MAUI one - catch `Microsoft.Maui.ApplicationModel.PermissionException` |
| `Permissions` (Windows only) | `Shiny` | `Microsoft.Maui.ApplicationModel` | Use `new ShinyPermissions.X().RequestAsync()` or the `Microsoft.Maui.ApplicationModel.Permissions` FQN |

**Safe global usings** (won't conflict with MAUI):
```csharp
global using Shiny;
global using Shiny.Extensions.Stores;   // IKeyValueStore, StoreKeys, [Bind]
global using Shiny.Jobs;
global using Shiny.Locations;
global using Shiny.BluetoothLE;
// Do NOT globally use: Shiny.Net, Shiny.Power, Shiny.Notifications, Shiny.Push, Shiny.BluetoothLE.Hosting
```

**Shiny 5.7.1 only:** `BatteryState`, `BatteryPowerSource`, `EnergySaverStatus` (Core), `AccessRequestFlags` (Notifications) and `GeofenceServiceCollectionExtensions` (Locations) shipped in the global namespace by mistake. A global type beats one imported by `using`, so in a MAUI app an unqualified `BatteryState` resolved to Shiny's enum instead of `Microsoft.Maui.Devices.BatteryState`. From 5.7.2 they are back in `Shiny.Power`, `Shiny.Notifications` and `Shiny` — always write the namespace import or FQN, never rely on the global resolution.

## Best Practices

- **Initialize Shiny early** -- `UseShiny()` must be called in the builder chain before building the MAUI app. For native apps, the host must be created and `Run()` called in the application startup.
- **Prefer attribute-based registration** -- tag services with `[Singleton]`/`[Scoped]`/`[Transient]` and let the `Shiny.Extensions.DependencyInjection` source generator emit `AddGeneratedServices()`. AOT-clean, no reflection at startup, multiple interfaces handled.
- **Prefer `[Bind]` partial properties** for persisted settings instead of `INotifyPropertyChanged` plumbing. The generator emits getters/setters that round-trip through the configured `IKeyValueStore`.
- **Keep startup tasks lightweight** -- `IShinyStartupTask.Start()` runs synchronously on the main thread at startup.
- **Use the right keyed store** -- `StoreKeys.Default` for general preferences (backed by SharedPreferences / NSUserDefaults / ApplicationData.LocalSettings / localStorage), `StoreKeys.Secure` for sensitive data (Android Keystore / iOS Keychain / Windows secure storage).
- **Use the static `Shiny.Stores.Default` / `Shiny.Stores.Secure` / `Shiny.Stores.Keyed(alias)` accessor** for one-off reads/writes outside DI.
- **Third-party containers and keyed services** -- Prism/DryIoc and other adapters that predate .NET 8 keyed services silently ignore `[FromKeyedServices]` and resolve the plain service type instead. `AddShinyStores()` registers the default `IKeyValueStore` unkeyed as well, so Shiny's own platform types still build on those containers. In *your* code, use the static `Shiny.Stores.Secure` / `Shiny.Stores.Keyed(...)` accessor rather than `[FromKeyedServices]` for non-default stores when the app uses a non-Microsoft container — a container that drops the key will inject the wrong store (or fail with `UnableToFindCtorWithAllResolvableArgs`).
- **Check `Host.IsInitialized`** before accessing `Host.Current` in code that may run before initialization.
- **Use `BindingList<T>`** for thread-safe observable collections that can be bound to UI.
- **Use the JSON contexts emitted by Shiny modules** if you mix `Shiny.Extensions.Serialization` with your own — Shiny.Jobs, Shiny.Gps, Shiny.Geofencing, Shiny.Notifications, and Shiny.Net.Http each ship their own `JsonSerializerContext` for AOT safety.

## Reference Files

- [API Reference](reference/api-reference.md)
- Public docs: https://shinylib.net/client/core/ (platform, lifecycle hooks, startup tasks, device monitoring, access & permissions, utilities, release notes)
