# Shiny.AppFunctions: plan and decisions

Status: **all phases built (2026-09-29), moved into this repo as `src/Shiny.AppFunctions*`.** Usage, what was verified and known limits: https://shinylib.net/client/appfunctions/ and `skills/shiny-appfunctions`. The Phase 0 spikes stayed in the Prototypes repo (`Shiny.AppFunctions/spikes`), as did the XCUITest Spotlight driver (`tools/OSDriver`).

**Changes from this plan made while building:**
- **Android conversion.** `GenericDocument` ↔ JSON is one converter driven by the generated descriptors, not per-type generated converters. It's still reflection-free and trim-safe, and a lot less generated code.
- **JSON.** The generator writes its own binding and result code (`AppFunctionArguments` + `Utf8JsonWriter`) and doesn't use a `JsonSerializerContext`. The System.Text.Json source generator can't see another generator's output.
- **Entity search on Android.** Each entity gets a generated `search_{id}` function, because Android has no entity queries. Entity lookups pass through the delegates too.
- **Initializer properties** are optional unless marked C# `required`. A value that isn't sent keeps the property's default.
- **`CFBundleDevelopmentRegion`** is added to the built `Info.plist` only when it's missing (with PlistBuddy, before codesign), so the app's own value wins.
- **Release Android** needed `[DynamicDependency]` on the service. It's only referenced from the manifest, so the trimmer removed it.
- **Delegate refusals** reach `OnInvoked` as an `AppFunctionException(Denied, message)`.

Decided: 2026-09-29.

## Goal

Declare an app's functions **once in C#** and expose them to:

- **Siri / Apple Intelligence** as App Intents (iOS),
- **Gemini** as AppFunctions (Android 16+),
- and, later, in-app AI tools and MCP, using the same generated registry.

Targets MAUI apps on iOS and Android, but the library has **no MAUI dependency**. It targets `net10.0`, `net10.0-ios` and `net10.0-android` and is hosted on **Shiny.Core**.

## Decisions

| # | Topic | Decision | Why |
|---|---|---|---|
| 1 | Hosting | **Shiny.Core host.** The runtime registers itself through an `IShinyStartupTask` | Intents arrive during background launches with no UI. Shiny.Core already builds the host on those paths (`IosLifecycle`, `AndroidPlatform`) |
| 2 | Declaring functions | **Record + typed handler.** `[AppFunction]` record implementing `IAppFunction<TResult>`, handled by `IAppFunctionHandler<TRequest, TResult>` | Strong typing, one handler per function, and the same shape as Shiny Mediator, so an adapter is easy later |
| 3 | Cross-cutting | **`IAppFunctionDelegate`**: several allowed, run in registration order, with `OnInvoking` (can deny) and `OnInvoked` (result or exception) | The classic Shiny delegate role: auth checks, feature flags, telemetry |
| 4 | Registration | **Source-generated `services.AddAppFunctions()`** registers every handler, entity query and delegate, the dispatch table and the JSON context | There will be many handlers and delegates, and hand-written registration drifts |
| 5 | v1 outputs | **Siri + Gemini.** The generated registry exposes each function's id, description and JSON schema so AI tools and MCP can be added without changing the generator | Keeps v1 focused, and keeps the extra targets cheap to add |
| 6 | Android API | **Platform `android.app.appfunctions` (API 36) directly.** No Jetpack or KSP. Below Android 16 it's a no-op | The bindings already ship in Mono.Android 36.1 (`Android.App.AppFunctions.*`). Replicating KSP output would be more work than it saves |
| 7 | No reflection | **Dispatch and JSON are fully generated** (a switch on function id, `JsonSerializerContext`) | Trim- and AOT-safe on iOS |
| 8 | iOS bridge | **Swift calls into C# through a function pointer that C# registers at startup** (`delegate* unmanaged`), not through an exported symbol | Works the same on Mono AOT, CoreCLR and NativeAOT. Same C ABI style as `wearables.h` |
| 9 | iOS process | **In-app only, no App Intents extension** | A second .NET runtime in an extension costs memory and binary size (see the widgets plan) |
| 10 | Handler scope | **One DI scope per invocation**, with a `CancellationToken` tied to the OS request (iOS task cancellation, Android `CancellationSignal`) | Handlers can take scoped services. The OS gives each call a limited time budget |

## Target usage

```csharp
[AppFunction("create_order", Title = "Create Order", Description = "Creates an order for a customer")]
public record CreateOrder(
    [property: AppParameter(Title = "Customer")] CustomerEntity Customer,
    int Quantity) : IAppFunction<OrderResult>;

public class CreateOrderHandler(IOrderService orders) : IAppFunctionHandler<CreateOrder, OrderResult>
{
    public async Task<OrderResult> Handle(CreateOrder request, AppFunctionContext ctx, CancellationToken ct)
    {
        var order = await orders.Create(request.Customer.Id, request.Quantity, ct);
        ctx.Say($"Order {order.Number} created");      // Siri dialog / Gemini response text
        return new OrderResult(order.Number);
    }
}

[AppEntity("customer", Title = "Customer", DisplayProperty = nameof(Name))]
public record CustomerEntity(string Id, string Name);

public class CustomerQuery(ICustomers customers) : IAppEntityQuery<CustomerEntity>
{
    public Task<IReadOnlyList<CustomerEntity>> GetByIds(string[] ids, CancellationToken ct) => ...;
    public Task<IReadOnlyList<CustomerEntity>> Search(string text, CancellationToken ct) => ...;
    public Task<IReadOnlyList<CustomerEntity>> Suggested(CancellationToken ct) => ...;
}

public class AuthDelegate(IAuth auth) : IAppFunctionDelegate
{
    public Task<AppFunctionGate> OnInvoking(AppFunctionContext ctx) =>
        Task.FromResult(auth.IsSignedIn ? AppFunctionGate.Allow : AppFunctionGate.OpenApp("Sign in first"));
    public Task OnInvoked(AppFunctionContext ctx, object? result, Exception? ex) => Task.CompletedTask;
}

// MauiProgram / host setup
builder.Services.AddAppFunctions();   // generated
```

Apple-only extras go in separate attributes that the Android build ignores, for example `[AppShortcut("Create an order in ${applicationName}")]` for Siri phrases.

## Shared model (v1)

The subset both platforms can express:

- **Functions:** id (`[a-z][a-z0-9_]*`, valid as both a Swift identifier and an Android function id), title, description, whether it opens the app.
- **Parameter types:** `string`, `int`, `long`, `double`, `bool`, `DateTimeOffset`, `enum` (generated as a Swift `AppEnum`), an `[AppEntity]` reference, and nullable versions of these. Anything else is a generator error.
- **Results:** `void`, or a record made of the same types, plus optional dialog text via `ctx.Say(...)`.
- **Errors:** `AppFunctionException(code, message)` maps to an iOS error dialog and to Android `AppFunctionException` categories.

## How the pieces fit

```
             C# (app assembly)
   [AppFunction] records, handlers, entities, queries, delegates
                        │
        Source generator (Roslyn, netstandard2.0)
   ├─ AddAppFunctions()      DI registration
   ├─ AppFunctionRegistry    id → typed invoker (switch), JSON schema per function
   ├─ JsonSerializerContext  every request, result and entity type
   ├─ diagnostics            duplicate ids, missing or duplicate handler, bad types
   └─ manifest consts        generated Swift source + Android XML, as string consts
                        │
        MSBuild task (after CoreCompile): reads the consts from the compiled IL, writes files
   ├─ iOS:      swiftc → static lib (NativeReference)
   │            appintentsmetadataprocessor → Metadata.appintents in the .app
   └─ Android:  app_functions.xml → AndroidAsset
```

**Why the generator produces the Swift and XML text:** Roslyn generators can only add C# to the compilation. Putting the output in `const string` fields keeps all the logic in the generator, where snapshot tests can cover it. The MSBuild task stays a small file writer that reads the constants with `System.Reflection.Metadata`. The trimmer removes the constants from Release builds.

### iOS runtime path

1. The OS launches or wakes the app. `FinishedLaunching` runs, the Shiny host builds, and the startup task calls `shiny_af_set_handler(ptr)`.
2. Swift `perform()` waits for the host-ready signal (with a timeout), then calls the handler pointer with `(functionId, requestJson, completionContext)`.
3. C# creates a DI scope, runs the delegates' `OnInvoking`, runs the handler, runs `OnInvoked`, then calls `shiny_af_complete(ctx, status, resultJson, dialog)`.
4. Swift resumes its `CheckedContinuation` and returns `.result(value:dialog:)`.

Each entity gets a Swift `AppEntity` struct plus an `EntityStringQuery` that bridges `entities(for:)`, `entities(matching:)` and `suggestedEntities()` to `IAppEntityQuery<T>` over the same channel.

### Android runtime path

1. The library ships `ShinyAppFunctionService : Android.App.AppFunctions.AppFunctionService`. It's declared with the `BIND_APP_FUNCTION_SERVICE` permission and has a manifest property that points at the generated `app_functions.xml` asset.
2. `Application.OnCreate` has already built the Shiny host by the time `OnExecuteFunction` runs.
3. The `GenericDocument` parameters are converted to the request record by generated converters, not reflection. The pipeline is the same as on iOS, and the result goes back as a `GenericDocument` through the `OutcomeReceiver`.

## Projects

```
Shiny.AppFunctions/
  Shiny.AppFunctions.slnx
  Directory.Build.props
  src/
    Shiny.AppFunctions/                    net10.0; net10.0-ios; net10.0-android
      Attributes, contracts, AppFunctionContext, the dispatcher pipeline
      Platforms/iOS      C ABI imports, startup task that registers the handler pointer
      Platforms/Android  ShinyAppFunctionService, GenericDocument helpers
      swift/             ShinyAppFunctions.swift runtime (bridge, continuation, host-ready wait)
      buildTransitive/   .props/.targets that wire in the MSBuild task
    Shiny.AppFunctions.SourceGenerators/   netstandard2.0, packed into analyzers/
    Shiny.AppFunctions.Build/              MSBuild task (net8.0)
  tests/
    Shiny.AppFunctions.Tests/              dispatcher and delegate pipeline (net10.0)
    Shiny.AppFunctions.SourceGenerators.Tests/  snapshot tests of generated C#, Swift and XML, plus diagnostics
  samples/
    Shiny.AppFunctions.Sample/             MAUI app (MAUI is only used by the sample)
```

## Build order (with go/no-go gates)

### Phase 0: spikes (gate)

Each spike is hand-written with no generator. If either fails, stop and rethink before building anything else.

- **iOS:** one hand-written Swift `AppIntent` compiled with swiftc (reusing Shiny.Wearables' `Exec` pattern) into a static lib linked into a MAUI app. Then `appintentsmetadataprocessor` runs against the linked binary using `-emit-const-values-path` output and writes `Metadata.appintents` before signing.
  - **Done when:** the action appears in the Shortcuts app on the simulator, running it calls C# through the registered pointer, and it works when the app starts cold from Shortcuts.
  - **Biggest risk in the project.** ✅ **Resolved:** the hook is `BeforeTargets="BeforeCodesign"`, and the swiftc flags and processor arguments were copied from an Xcode 27 build log. A Spotlight-triggered cold start ran the C# handler on the simulator.
  - Findings that feed phase 4: add `CFBundleDevelopmentRegion` or warn when it's missing, don't hard-code the const-extract protocol list, always emit `parameterSummary`, and use `LinkWithSwiftSystemLibraries` on the `NativeReference`. The spike README has the full list.
- **Android:** a C# `AppFunctionService` subclass, a hand-written XML asset and a manifest property on an API 36 emulator.
  - **Done when:** `adb shell cmd app_function` lists the function and executing it returns a `GenericDocument` built in C#.
  - **Check** which XML schema version Gemini reads today, since the platform's v1 and v2 formats differ.
  - ✅ **Passed:** the system indexed and executed all 3 functions from a cold start, including a nested object result and error mapping. The generator will emit the Jetpack layout: the v1 id list, `app_functions_v2.xml` (typed), and the Jetpack XSD, referenced by three manifest `<property>` elements. The type codes and runtime mapping are in the spike README.
  - Gemini itself was not exercised: the emulator has no Gemini, and agent calls are allowlisted by the system. That needs a physical device and stays an open check before phase 3 is called done.

### Phase 1: runtime core (net10.0)

- Attributes, `IAppFunction<T>`, `IAppFunctionHandler<,>`, `IAppEntityQuery<T>`, `IAppFunctionDelegate`, `AppFunctionContext`, `AppFunctionException`.
- `AppFunctionDispatcher`: scope per call, delegate pipeline, error mapping. It works against an `IAppFunctionRegistry` abstraction.
- **Done when:** unit tests cover allow, deny, open-app, exception and cancellation paths with a hand-written registry.

### Phase 2: source generator

- Incremental generator built on `ForAttributeWithMetadataName`. It emits `AddAppFunctions()`, the registry (switch dispatch plus a JSON schema per function), the `JsonSerializerContext`, and the Swift and XML consts.
- Diagnostics: duplicate function id, no handler, more than one handler, unsupported parameter type, entity with no query, invalid id, delegate with no public constructor.
- **Done when:** snapshot tests pass for a representative model and every diagnostic has a test.

### Phase 3: Android output

- The service and its manifest entry live in the **library's own `AndroidManifest.xml`** and are merged into the app, so apps add nothing (proven in Phase 0). The XSD ships in `buildTransitive` as an `AndroidAsset`.
- The MSBuild task writes `app_functions.xml` and `app_functions_v2.xml` as `AndroidAsset` items. The generated `GenericDocument` converters validate required parameters, because the system doesn't.
- **Done when:** the sample's generated functions list and execute through `adb`, and Gemini sees them on a supported device if one is available.

### Phase 4: iOS output

- MSBuild task writes the generated Swift, compiles it together with the runtime Swift into a static lib, adds a `NativeReference`, and runs the metadata processor.
- Generates App Shortcuts from `[AppShortcut]`.
- **Done when:** the sample's functions and entities work from Shortcuts and Siri on the simulator, including a cold start, entity search and a denial from a delegate.

### Phase 5: sample, README, AI-tool seam

- The MAUI sample covers orders, customers and a sign-in delegate.
- `IAppFunctionRegistry.Functions` exposes the id, description and JSON schema, which is enough for a later `Microsoft.Extensions.AI` `AIFunction` adapter and a `Shiny.Net.HttpServer.Mcp` adapter.
- Add a row to the root README.

## Out of scope for v1

- App Intents extensions, interactive snippets, Controls, Focus filters, Spotlight `IndexedEntity`, and Apple assistant schemas (`@AssistantIntent`).
- The Jetpack `androidx.appfunctions` library and Android below 16.
- Handlers declared in referenced class libraries. v1 scans only the app assembly, and a diagnostic points this out when a library has any.
- Localized titles and descriptions. v1 uses literal strings. `.resx` support comes later.
- The AI-tool and MCP adapters themselves (the seam ships in v1).

## Settled follow-ups

1. **Package id and folder:** `Shiny.AppFunctions`.
2. **Minimum iOS:** 16.0 (App Intents plus App Shortcuts). Lower OS versions no-op at runtime.
3. **Library handlers:** v1 scans only the app assembly, as listed under "Out of scope for v1".
4. **"Opens app" functions** (`[AppFunction(OpensApp = true)]` or `AppFunctionGate.OpenApp`): the handler runs *after* the app is in the foreground, and navigation is left to the app. `AppFunctionContext.IsForeground` tells the handler which case it's in. A separate `OnOpenApp` delegate event can be added later if apps need it.
