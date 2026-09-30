# Shiny.AppFunctions API Reference

## Installation

```xml
<PackageReference Include="Shiny.AppFunctions" Version="5.*" />
```

Targets: `net10.0` (contracts + dispatcher, no platform bridge), `net10.0-ios`, `net10.0-android`.
The package also carries the source generator (`analyzers/`), the MSBuild task (`tasks/`) and the build targets,
Swift runtime and Android schema (`buildTransitive/`).

## Namespaces

```csharp
using Shiny.AppFunctions;                          // everything
// AddAppFunctions() is generated into Microsoft.Extensions.DependencyInjection
```

## Registration

```csharp
// generated into the app project
internal static class ShinyAppFunctionsRegistration
{
    // every handler + entity query (scoped), every delegate (scoped, TryAddEnumerable), and the runtime
    public static IServiceCollection AddAppFunctions(this IServiceCollection services);
}

public static class AppFunctionsServiceCollectionExtensions
{
    // called by the generated AddAppFunctions(); only call it yourself with a hand-written registry (tests)
    [EditorBrowsable(Never)]
    public static IServiceCollection AddAppFunctionsRuntime<TRegistry>(this IServiceCollection services) where TRegistry : class, IAppFunctionRegistry;
}
```

## Attributes

```csharp
[AttributeUsage(Class | Struct)]
public sealed class AppFunctionAttribute(string id) : Attribute
{
    public string Id { get; }
    public string? Title { get; set; }          // default: type name split into words
    public string? Description { get; set; }
    public bool OpensApp { get; set; }         // needs the app on screen: iOS foregrounds it first; Android/other runs only if visible, else Denied
}

[AttributeUsage(Property | Parameter)]
public sealed class AppParameterAttribute : Attribute
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}

[AttributeUsage(Class | Struct)]
public sealed class AppEntityAttribute(string id) : Attribute
{
    public string Id { get; }
    public string? Title { get; set; }
    public string? DisplayProperty { get; set; }   // default: Name, then Title, then Id
}

[AttributeUsage(Class | Struct, AllowMultiple = true)]   // Apple only
public sealed class AppShortcutAttribute(string phrase) : Attribute
{
    public string Phrase { get; }              // must contain ${applicationName}
    public string? ShortTitle { get; set; }
    public string? SystemImage { get; set; }   // SF Symbol
}
```

## Contracts

```csharp
public interface IAppFunction;
public interface IAppFunction<TResult>;

public interface IAppFunctionHandler<in TRequest> where TRequest : IAppFunction
{
    Task Handle(TRequest request, AppFunctionContext context, CancellationToken cancellationToken);
}

public interface IAppFunctionHandler<in TRequest, TResult> where TRequest : IAppFunction<TResult>
{
    Task<TResult> Handle(TRequest request, AppFunctionContext context, CancellationToken cancellationToken);
}

public interface IAppEntityQuery<TEntity>
{
    Task<IReadOnlyList<TEntity>> GetByIds(IReadOnlyList<string> ids, CancellationToken cancellationToken);
    Task<IReadOnlyList<TEntity>> Search(string text, CancellationToken cancellationToken);
    Task<IReadOnlyList<TEntity>> Suggested(CancellationToken cancellationToken);   // default: empty
}

public interface IAppFunctionDelegate
{
    Task<AppFunctionGate> OnInvoking(AppFunctionContext context, CancellationToken cancellationToken);   // default: Allow
    Task OnInvoked(AppFunctionContext context, object? result, Exception? exception);                    // default: nothing
}

public enum AppFunctionGateKind { Allow, Deny, OpenApp }

public sealed class AppFunctionGate
{
    public AppFunctionGateKind Kind { get; }
    public string? Message { get; }
    public static AppFunctionGate Allow { get; }
    public static AppFunctionGate Deny(string message);
    public static AppFunctionGate OpenApp(string message);   // passes if IsForeground; else iOS: continue in foreground and re-run; Android: Denied
}
```

## Context

```csharp
public enum AppFunctionPlatform { Other, Apple, Android }

public sealed record AppFunctionInvocation(
    string FunctionId,
    AppFunctionPlatform Platform = AppFunctionPlatform.Other,
    bool IsForeground = false,
    string? CallerPackage = null
);

public sealed class AppFunctionContext
{
    public AppFunctionDescriptor Function { get; }
    public AppFunctionInvocation Invocation { get; }
    public string FunctionId { get; }
    public AppFunctionPlatform Platform { get; }
    public bool IsForeground { get; }                   // app on screen: user asked from inside it, in-app AI tools, or iOS OpensApp/OpenApp
    public string? CallerPackage { get; }               // Android: calling agent's package
    public object? Request { get; }                     // null for entity lookups
    public IServiceProvider Services { get; }           // the call's DI scope
    public IDictionary<string, object?> Items { get; }
    public string? Dialog { get; }
    public void Say(string dialog);
}
```

## Errors

```csharp
public enum AppFunctionErrorCode { InvalidArgument, NotFound, Denied, Cancelled, AppError }

public class AppFunctionException(AppFunctionErrorCode code, string message, Exception? innerException = null) : Exception
{
    public AppFunctionErrorCode Code { get; }
}
```

## Dispatcher (in-process calls)

```csharp
public enum AppFunctionStatus { Success, Error, NeedsForeground }

public sealed record AppFunctionOutcome(
    AppFunctionStatus Status,
    string? ResultJson = null,
    string? Dialog = null,
    AppFunctionErrorCode? ErrorCode = null,
    string? Message = null
);

public sealed class AppFunctionDispatcher      // singleton
{
    public const string EntityOperationPrefix = "entity:";   // entity:{entityId}:{ids|search|suggested}
    public IAppFunctionRegistry Registry { get; }
    public Task<AppFunctionOutcome> Execute(AppFunctionInvocation invocation, string argumentsJson, CancellationToken cancellationToken);   // never throws
}

public static class AppFunctionsHost
{
    public static bool IsReady { get; }
    public static TimeSpan ReadyTimeout { get; set; }        // 10 s - how long OS calls wait for the host
    public static Task<AppFunctionDispatcher> WaitForDispatcher(CancellationToken cancellationToken = default);
}
```

## Registry & descriptors

```csharp
public interface IAppFunctionRegistry           // generated
{
    IReadOnlyList<AppFunctionDescriptor> Functions { get; }   // declared, then search_{entity}
    IReadOnlyList<AppEntityDescriptor> Entities { get; }
    // + binding / invoke / result-writing / entity-query members used by the dispatcher
}

public sealed class AppFunctionDescriptor
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public bool OpensApp { get; }
    public IReadOnlyList<AppParameterDescriptor> Parameters { get; }
    public AppTypeDescriptor Result { get; }
    public string? SearchesEntityId { get; }   // set on generated search_{entity} functions
    public string GetParametersJsonSchema();   // JSON schema draft 2020-12
}

public sealed class AppParameterDescriptor { string Name; string Title; string? Description; AppTypeDescriptor Type; bool IsRequired; }
public sealed class AppEntityDescriptor { string Id; string Title; string TypeName; }
public enum AppValueKind { Void, String, Int32, Int64, Double, Boolean, DateTimeOffset, Enum, Entity, Object, Array }
public readonly record struct AppEntityItem(string Id, string Title);
```

## Android

```csharp
[Register("shiny.appfunctions.ShinyAppFunctionService")]
public class ShinyAppFunctionService : AppFunctionService    // declared by the library manifest, merged into the app
{
    public const string DialogExtraKey = "shiny.appfunctions.dialog";   // response extras: context.Say(...) text
}
```

Generated assets: `app_functions.xml`, `app_functions_v2.xml`, `app_functions_schema.xsd`.

## Wire format

- Parameter names are the property names in **camelCase**.
- Enums: the member name as a string (case-insensitive when binding).
- `DateTimeOffset`: ISO 8601 (epoch milliseconds also accepted).
- Numbers and booleans are also accepted as strings.
- Entities: the entity id as a string.

## AI tools (Shiny.AppFunctions.Extensions.AI)

```xml
<PackageReference Include="Shiny.AppFunctions.Extensions.AI" Version="5.*" />
```

Target: `net10.0` (usable from the iOS and Android heads). Depends on `Microsoft.Extensions.AI.Abstractions`.

```csharp
namespace Shiny;
public static class AppFunctionsAiServiceCollectionExtensions
{
    // requires AddAppFunctions(); throws if the builder added nothing
    public static IServiceCollection AddAppFunctionAITools(this IServiceCollection services, Action<IAppFunctionAIToolBuilder> configure);
}

namespace Shiny.AppFunctions.Extensions.AI;
public interface IAppFunctionAIToolBuilder
{
    IAppFunctionAIToolBuilder AddAllFunctions();                        // includes search_{entity}
    IAppFunctionAIToolBuilder AddFunction(string functionId);            // + search_{entity} for entity parameters
    IAppFunctionAIToolBuilder AddFunctions(IEnumerable<string> functionIds);
    IAppFunctionAIToolBuilder ExcludeFunction(string functionId);        // wins over All and entity searches
}

public sealed class AppFunctionAITools                                  // singleton; unknown ids throw on first resolve
{
    public IReadOnlyList<AITool> Tools { get; }                          // AIFunction per function, registry order
}
```

Tool results (`JsonObject`):
- success: `{ "success": true, "result": <value>?, "message": "<dialog>"? }`
- failure: `{ "error": "<message>", "code": "<AppFunctionErrorCode>" }`

Calls run as `AppFunctionInvocation(id, IsForeground: true)` - `AppFunctionPlatform.Other`, in the foreground, so `OpensApp` functions run and `OpenApp` gates pass.
