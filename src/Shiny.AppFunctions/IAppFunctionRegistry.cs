using System.Text.Json;

namespace Shiny.AppFunctions;

/// <summary>
/// Everything the app declared, plus reflection-free binding. Implemented by the source generator
/// (<c>AddAppFunctions()</c> registers it); implement it yourself only in tests.
/// </summary>
public interface IAppFunctionRegistry
{
    /// <summary>Declared functions, followed by one generated <c>search_{entity}</c> function per entity.</summary>
    IReadOnlyList<AppFunctionDescriptor> Functions { get; }

    IReadOnlyList<AppEntityDescriptor> Entities { get; }

    /// <summary>
    /// Binds wire arguments (a JSON object keyed by parameter name) to the request record, resolving entity ids
    /// through their queries. Throws <see cref="AppFunctionException"/> with <see cref="AppFunctionErrorCode.InvalidArgument"/>
    /// for missing or malformed arguments.
    /// </summary>
    ValueTask<object> CreateRequest(string functionId, JsonElement arguments, IServiceProvider services, CancellationToken cancellationToken);

    /// <summary>Resolves the handler from <see cref="AppFunctionContext.Services"/> and runs it. Returns null for functions without a result.</summary>
    Task<object?> Invoke(object request, AppFunctionContext context, CancellationToken cancellationToken);

    /// <summary>Writes a handler result as a JSON value. Not called for functions without a result.</summary>
    void WriteResult(string functionId, object? result, Utf8JsonWriter writer);

    Task<IReadOnlyList<AppEntityItem>> QueryEntities(string entityId, AppEntityQueryKind kind, IReadOnlyList<string> arguments, IServiceProvider services, CancellationToken cancellationToken);
}
