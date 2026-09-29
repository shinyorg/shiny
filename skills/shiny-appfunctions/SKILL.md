---
name: shiny-appfunctions
description: Guide for exposing a .NET MAUI / .NET app's functions to Siri, Spotlight, Shortcuts and Apple Intelligence (App Intents) and to Gemini and other agents (Android AppFunctions) from one C# declaration using Shiny.AppFunctions - [AppFunction] records, typed handlers, [AppEntity] entities with queries, [AppShortcut] Siri phrases and delegates
auto_invoke: true
triggers:
  - app functions
  - appfunctions
  - app function
  - app intents
  - app intent
  - AppIntent
  - AppIntents
  - siri
  - siri shortcut
  - siri phrase
  - hey siri
  - apple intelligence
  - shortcuts app
  - spotlight action
  - app shortcut
  - AppShortcutsProvider
  - gemini
  - android appfunctions
  - AppFunctionService
  - agent can call my app
  - expose app to assistant
  - assistant integration
  - voice assistant
  - AppFunctionAttribute
  - AppFunction
  - AppParameter
  - AppEntity
  - AppShortcut
  - IAppFunction
  - IAppFunctionHandler
  - IAppEntityQuery
  - IAppFunctionDelegate
  - AppFunctionGate
  - AppFunctionContext
  - AppFunctionException
  - AppFunctionErrorCode
  - AppFunctionDispatcher
  - AppFunctionInvocation
  - AppFunctionOutcome
  - IAppFunctionRegistry
  - AppFunctionsHost
  - AddAppFunctions
  - ShinyAppFunctionService
  - ShinyAppFunctionsEnabled
  - SHAF001
  - Shiny.AppFunctions
  - shiny app functions
---

# Shiny App Functions

## When to Use This Skill

Use this skill when the user needs to:
- Let Siri, Spotlight, the Shortcuts app or Apple Intelligence run something in their app (App Intents)
- Let Gemini or another Android agent call into their app (Android 16+ AppFunctions)
- Add Siri phrases / App Shortcuts that work without the user setting anything up
- Let the assistant pick an app object (a customer, a playlist) as a parameter
- Gate assistant calls (sign-in, feature flags) or log/telemetry every call
- Call the same functions in-process (tests, an in-app AI tool, MCP) with a JSON schema

## Library Overview

| Item | Value |
|------|-------|
| **NuGet** | `Shiny.AppFunctions` (the source generator and the MSBuild task ship inside it) |
| **Namespace** | `Shiny.AppFunctions` |
| **Registration** | `services.AddAppFunctions()` - **source-generated** into the app, in `Microsoft.Extensions.DependencyInjection` |
| **Host** | Shiny.Core (`.UseShiny()` on MAUI) - no MAUI dependency |
| **Platforms** | iOS 16+ (App Intents), Android 16 / API 36+ (AppFunctions; a no-op below). `net10.0` carries the contracts and the dispatcher - no platform bridge. No Mac Catalyst/macOS/Windows bridge |

**The user writes no Swift, Kotlin, Info.plist, entitlements or AndroidManifest entries.** The generator writes the DI registration, binding, dispatch, the Swift App Intents and the Android schema; the package's build targets compile the Swift into the iOS app (and run Apple's metadata + Siri phrase processors) and add the schema assets on Android. The Android service arrives through the manifest merger.

## Setup

```csharp
// MauiProgram.cs
builder
    .UseMauiApp<App>()
    .UseShiny();                          // required - the runtime starts from a Shiny startup task

builder.Services.AddAppFunctions();       // generated: every handler, entity query and delegate in this project
```

## Declaring functions - CRITICAL rules

1. **Declare everything in the APP project** (the head with `OutputType=Exe` / the Android application). The generator only scans the project it runs in - `[AppFunction]` records, handlers, `[AppEntity]` records, `IAppEntityQuery<T>` and `IAppFunctionDelegate` classes in a class library are **not found**. Services, enums and result records used by them may come from any library.
2. A function is a **record marked `[AppFunction("id")]` implementing `IAppFunction` (no result) or `IAppFunction<TResult>`** - exactly one of them.
3. Ids: **lowercase letters, digits and underscores, starting with a letter** (`create_order`). Shared by both platforms; changing one breaks saved Shortcuts and agent references.
4. **Exactly one handler per function**: `IAppFunctionHandler<TRequest>` or `IAppFunctionHandler<TRequest, TResult>`, a public non-abstract class with a public constructor. Resolved from a **new DI scope per call** - scoped services are fine.
5. Parameters are the record's properties:
   - constructor parameters are **required unless nullable**;
   - settable / `init` properties are **optional unless C# `required`** (a value not sent keeps the property default);
   - computed (get-only, not in the constructor) properties are ignored.
6. Describe things with `Description` - assistants decide when to call a function from it.

```csharp
using Shiny.AppFunctions;

public enum Priority { Low, Normal, Urgent }                          // → Swift AppEnum / Android enumValues

[AppEntity("customer", Title = "Customer")]                            // needs a public string Id
public record Customer(string Id, string Name, string City);          // display: DisplayProperty, else Name, Title, Id

public class CustomerQuery(ICustomers customers) : IAppEntityQuery<Customer>
{
    public Task<IReadOnlyList<Customer>> GetByIds(IReadOnlyList<string> ids, CancellationToken ct) => customers.ByIds(ids, ct);
    public Task<IReadOnlyList<Customer>> Search(string text, CancellationToken ct) => customers.Search(text, ct);
    public Task<IReadOnlyList<Customer>> Suggested(CancellationToken ct) => customers.Recent(ct);   // optional (default: empty)
}

public record OrderResult(string Number, int Quantity, double Total);

[AppFunction("create_order", Description = "Creates an order for a customer")]
[AppShortcut("Create an order in ${applicationName}", ShortTitle = "New Order", SystemImage = "cart.badge.plus")]
public record CreateOrder(
    [property: AppParameter(Title = "Customer", Description = "Who the order is for")] Customer Customer,
    int Quantity,
    Priority Priority,
    string? Note                                                       // nullable → optional
) : IAppFunction<OrderResult>;

public class CreateOrderHandler(IOrders orders) : IAppFunctionHandler<CreateOrder, OrderResult>
{
    public async Task<OrderResult> Handle(CreateOrder request, AppFunctionContext context, CancellationToken ct)
    {
        if (request.Quantity < 1)
            throw new AppFunctionException(AppFunctionErrorCode.InvalidArgument, "Quantity must be at least 1");  // message is shown/spoken

        var order = await orders.Create(request.Customer.Id, request.Quantity, request.Priority, request.Note, ct);
        context.Say($"Order {order.Number} is in.");                   // Siri dialog; Android returns it in the response extras
        return new OrderResult(order.Number, order.Quantity, order.Total);
    }
}

[AppFunction("count_open_orders", Title = "Open Orders", Description = "Counts the orders that have not shipped")]
[AppShortcut("How many orders are open in ${applicationName}", SystemImage = "shippingbox")]
public record CountOpenOrders : IAppFunction<int>;
```

### Supported types

| | Parameters | Results |
|---|---|---|
| `string`, `int`, `long`, `double`, `bool`, `DateTimeOffset` | ✅ (nullable = optional) | ✅ |
| enums (wire value = member name) | ✅ | ✅ |
| `[AppEntity]` records | ✅ (id on the wire, resolved through `IAppEntityQuery<T>`; unknown id → `NotFound`) | as plain objects |
| records / classes of the above | ❌ | ✅ (nested up to 4 levels) |
| arrays / lists | ❌ | ✅ |
| no result | | `IAppFunction` + `IAppFunctionHandler<TRequest>` |

**No `Guid`, `DateTime`, `decimal`, `float`, collections or nested objects as parameters** - use `string` / `double` / `DateTimeOffset` or an entity.

iOS: a primitive result is returned typed; an object or list result is returned as JSON text, and the dialog is whatever `context.Say(...)` set.

### Entities
- Each `[AppEntity]` needs an `IAppEntityQuery<T>` in the app project (SHAF009 otherwise).
- iOS uses the query for the App Entity picker (`Suggested` before typing, `Search` while typing, `GetByIds` to rehydrate).
- **Android has no entity queries**, so each entity also gets a generated **`search_{id}`** function (returns `[{id, title}]`) that agents call to find ids.
- Entity lookups pass through the delegates too.

### App Shortcuts (Apple only, ignored on Android)
- `[AppShortcut("phrase")]` - **every phrase must contain `${applicationName}`** (SHAF011). Repeat the attribute for more phrases; the first one's `ShortTitle` / `SystemImage` (an SF Symbol) are used.
- **At most 10 functions** can have App Shortcuts (SHAF012).
- `CFBundleDevelopmentRegion` is added to the built Info.plist (default `en`) when missing, because Siri phrase training needs it.

## Delegates - gating and observing every call

```csharp
public class SignInDelegate(IAuth auth) : IAppFunctionDelegate
{
    public Task<AppFunctionGate> OnInvoking(AppFunctionContext context, CancellationToken ct)
        => Task.FromResult(context.FunctionId == "cancel_order" && !auth.IsSignedIn
            ? AppFunctionGate.OpenApp("Sign in to cancel orders.")
            : AppFunctionGate.Allow);
}

public class TelemetryDelegate(ILogger<TelemetryDelegate> logger) : IAppFunctionDelegate
{
    public Task OnInvoked(AppFunctionContext context, object? result, Exception? exception)
    {
        logger.LogInformation("{Fn} from {Platform}: {Outcome}", context.FunctionId, context.Platform, exception?.Message ?? "ok");
        return Task.CompletedTask;
    }
}
```

- Delegates are **found and registered by the generator** - never register them yourself. Both methods have default implementations; override only what you need.
- They are scoped to the call and run in the order `AddAppFunctions()` registers them; the first `OnInvoking` that does not return `Allow` wins. Don't make one delegate depend on another having run first.
- `AppFunctionGate.Deny(message)` refuses (`Denied`). `AppFunctionGate.OpenApp(message)`: **iOS** asks the user to continue in the app, then runs the call again in the foreground (`context.IsForeground == true`); **Android** refuses with the message. Anything already foreground passes an `OpenApp` gate.
- A refusal reaches `OnInvoked` as `AppFunctionException(Denied, message)`. Exceptions thrown from `OnInvoked` are logged and ignored.
- `AppFunctionContext`: `FunctionId`, `Function` (descriptor), `Platform` (`Apple`/`Android`/`Other`), `IsForeground`, `CallerPackage` (Android caller), `Request`, `Services` (the call's scope), `Items` (shared with the handler), `Say(dialog)` / `Dialog`.
- `[AppFunction(OpensApp = true)]` brings the app to the foreground before the handler runs on iOS (Android runs it in the background).

## Errors

Throw `AppFunctionException(code, message)` - **write the message for the user**. Any other exception becomes `AppError`.

| `AppFunctionErrorCode` | Android `AppFunctionError` | Use for |
|---|---|---|
| `InvalidArgument` | `InvalidArgument` | bad / missing input (binding throws this itself for missing required values) |
| `NotFound` | `InvalidArgument` (unknown entity) / `FunctionNotFound` (unknown function) | the thing asked about doesn't exist |
| `Denied` | `Denied` | not allowed |
| `Cancelled` | `Cancelled` | caller cancelled / OS time budget ran out |
| `AppError` | `AppUnknownError` | anything else |

On iOS the message becomes the Siri error dialog.

## Calling functions in-process

The same pipeline Siri and Gemini use (new scope → bind → delegates → handler → JSON result). It never throws.

```csharp
public class MyViewModel(AppFunctionDispatcher dispatcher)
{
    async Task Run()
    {
        var outcome = await dispatcher.Execute(
            new AppFunctionInvocation("create_order"),                     // Platform = Other, IsForeground = false
            """{"customer":"acme","quantity":2,"priority":"Normal"}""",
            CancellationToken.None
        );
        // outcome.Status (Success/Error/NeedsForeground), ResultJson, Dialog, ErrorCode, Message
    }
}
```

`dispatcher.Registry.Functions` lists every function (declared ones, then the generated `search_*`), each with `GetParametersJsonSchema()` (JSON schema draft 2020-12) - the hook for an in-app AI tool or MCP adapter.

## Build properties (all optional)

| Property | Default | |
|---|---|---|
| `ShinyAppFunctionsEnabled` | `true` | `false` skips Swift/metadata/assets generation |
| `ShinyAppFunctionsMinimumOSVersion` | app's `SupportedOSPlatformVersion`, at least `16.0` | iOS deployment target of the Swift intents |
| `ShinyAppFunctionsDevelopmentRegion` | `en` | added to Info.plist only if it has no `CFBundleDevelopmentRegion` |
| `ShinyAppFunctionsConstProtocolsFile` | the Xcode 27 list shipped in the package | swiftc const-extraction protocol list; override for a later Xcode |

## Generator diagnostics

| Id | Meaning |
|---|---|
| SHAF001 | invalid function/entity id |
| SHAF002 | `[AppFunction]` type implements neither/both of `IAppFunction`, `IAppFunction<T>` |
| SHAF003 | unsupported parameter/result type |
| SHAF004 | request can't be constructed (no matching public constructor / settable properties) |
| SHAF005 / SHAF006 | no handler / more than one handler |
| SHAF007 / SHAF008 | entity has no public `string Id` / display property not found |
| SHAF009 | entity has no `IAppEntityQuery<T>` |
| SHAF010 | duplicate id |
| SHAF011 | App Shortcut phrase without `${applicationName}` |
| SHAF012 | more than 10 App Shortcuts |
| SHAF013 | handler/query/delegate has no public constructor |

## Testing

```bash
# Android 16+ emulator/device - every call can be a cold start
adb shell cmd app_function list-app-functions --package <applicationId>
adb shell "cmd app_function execute-app-function --package <applicationId> --function create_order --parameters '{\"customer\":\"acme\",\"quantity\":2,\"priority\":\"Normal\"}'"
```

- iOS: install on a simulator and search a shortcut's `ShortTitle` / phrase in Spotlight, or use the Shortcuts app.
- A Debug APK installed with plain `adb install` must set `<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>` - the system starts the process without Fast Deployment's files. `dotnet build -t:Run` / `-t:Install` is fine.
- Unit-test handlers directly, or the whole pipeline through `AppFunctionDispatcher`.

## Best Practices

- Keep handlers fast - the OS gives each call a limited time budget; honour the `CancellationToken`.
- Persist state that calls change: Siri and Gemini often start the app cold, in the background, with no UI.
- Put user-facing text in `context.Say(...)` and `AppFunctionException` messages.
- Don't touch UI from a handler - there may be no window. Raise an event and let the page refresh on the main thread.
- Never register handlers, queries or delegates manually - `AddAppFunctions()` does it (handlers and queries scoped, delegates scoped via `TryAddEnumerable`); a second registration with another lifetime runs a delegate twice.

## Known limits

- Functions in class libraries are not scanned (no diagnostic yet).
- Titles and descriptions are not localized.
- Multi-RID iOS builds (arm64 + x64 simulator together) are not supported by the metadata step.
- Siri by voice, Gemini itself and physical devices were not part of the verification; the emulator/simulator flows were.

## Reference Files

- [API Reference](reference/api-reference.md)
