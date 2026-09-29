using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.AppFunctions;

public enum AppFunctionStatus
{
    Success,
    Error,
    /// <summary>iOS only: the call must continue with the app in the foreground (see <see cref="AppFunctionGate.OpenApp"/>).</summary>
    NeedsForeground
}

/// <summary>
/// What the platform layer sends back to Siri / the agent. <see cref="ResultJson"/> is the result as a JSON value;
/// null for functions without a result and for failures.
/// </summary>
public sealed record AppFunctionOutcome(
    AppFunctionStatus Status,
    string? ResultJson = null,
    string? Dialog = null,
    AppFunctionErrorCode? ErrorCode = null,
    string? Message = null
)
{
    public static AppFunctionOutcome Failed(AppFunctionErrorCode code, string message) => new(AppFunctionStatus.Error, ErrorCode: code, Message: message);
}

/// <summary>
/// The one pipeline behind every platform: new DI scope → bind arguments → delegates' <c>OnInvoking</c> →
/// handler → delegates' <c>OnInvoked</c> → JSON result. Never throws; failures become an <see cref="AppFunctionOutcome"/>.
/// </summary>
public sealed class AppFunctionDispatcher(
    IServiceProvider services,
    IAppFunctionRegistry registry,
    ILogger<AppFunctionDispatcher> logger
)
{
    /// <summary>Prefix for the entity lookups App Intents makes: <c>entity:{entityId}:{ids|search|suggested}</c>.</summary>
    public const string EntityOperationPrefix = "entity:";

    public IAppFunctionRegistry Registry => registry;

    public async Task<AppFunctionOutcome> Execute(AppFunctionInvocation invocation, string argumentsJson, CancellationToken cancellationToken)
    {
        JsonDocument arguments;
        try
        {
            arguments = JsonDocument.Parse(String.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
        }
        catch (JsonException ex)
        {
            return AppFunctionOutcome.Failed(AppFunctionErrorCode.InvalidArgument, $"Arguments are not valid JSON: {ex.Message}");
        }

        using (arguments)
        {
            if (arguments.RootElement.ValueKind != JsonValueKind.Object)
                return AppFunctionOutcome.Failed(AppFunctionErrorCode.InvalidArgument, "Arguments must be a JSON object");

            if (invocation.FunctionId.StartsWith(EntityOperationPrefix, StringComparison.Ordinal))
                return await this.ExecuteEntityOperation(invocation, arguments.RootElement, cancellationToken).ConfigureAwait(false);

            var function = registry.Functions.FirstOrDefault(x => x.Id == invocation.FunctionId);
            if (function == null)
                return AppFunctionOutcome.Failed(AppFunctionErrorCode.NotFound, $"Unknown function '{invocation.FunctionId}'");

            if (function.SearchesEntityId != null)
            {
                var query = arguments.RootElement.TryGetProperty("query", out var q) && q.ValueKind == JsonValueKind.String ? q.GetString()! : "";
                return await this.QueryEntities(invocation, function, function.SearchesEntityId, AppEntityQueryKind.Search, [query], cancellationToken).ConfigureAwait(false);
            }

            return await this.ExecuteFunction(invocation, function, arguments.RootElement, cancellationToken).ConfigureAwait(false);
        }
    }

    async Task<AppFunctionOutcome> ExecuteFunction(AppFunctionInvocation invocation, AppFunctionDescriptor function, JsonElement arguments, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = new AppFunctionContext(function, invocation, scope.ServiceProvider);
        object? result = null;
        Exception? failure = null;
        var delegates = scope.ServiceProvider.GetServices<IAppFunctionDelegate>().ToList();

        try
        {
            context.Request = await registry.CreateRequest(function.Id, arguments, scope.ServiceProvider, cancellationToken).ConfigureAwait(false);

            var gate = await Gate(delegates, context, cancellationToken).ConfigureAwait(false);
            if (gate != null)
            {
                failure = Blocked(gate);
                return gate;
            }

            logger.LogDebug("Running app function {FunctionId} ({Platform})", function.Id, invocation.Platform);
            result = await registry.Invoke(context.Request, context, cancellationToken).ConfigureAwait(false);

            string? json = null;
            if (function.Result.Kind != AppValueKind.Void)
                json = Write(w => registry.WriteResult(function.Id, result, w));

            return new AppFunctionOutcome(AppFunctionStatus.Success, json, context.Dialog);
        }
        catch (Exception ex)
        {
            failure = ex;
            return this.ToOutcome(function.Id, ex, cancellationToken);
        }
        finally
        {
            await this.Invoked(delegates, context, result, failure).ConfigureAwait(false);
        }
    }

    async Task<AppFunctionOutcome> ExecuteEntityOperation(AppFunctionInvocation invocation, JsonElement arguments, CancellationToken cancellationToken)
    {
        // entity:{entityId}:{operation}
        var parts = invocation.FunctionId.Split(':');
        var entityId = parts.Length == 3 ? parts[1] : "";
        var search = registry.Functions.FirstOrDefault(x => x.SearchesEntityId == entityId);
        if (search == null)
            return AppFunctionOutcome.Failed(AppFunctionErrorCode.NotFound, $"Unknown entity '{entityId}'");

        switch (parts[2])
        {
            case "ids":
                var ids = arguments.TryGetProperty("ids", out var list) && list.ValueKind == JsonValueKind.Array
                    ? list.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList()
                    : [];
                return await this.QueryEntities(invocation, search, entityId, AppEntityQueryKind.ByIds, ids, cancellationToken).ConfigureAwait(false);

            case "search":
                var query = arguments.TryGetProperty("query", out var q) && q.ValueKind == JsonValueKind.String ? q.GetString()! : "";
                return await this.QueryEntities(invocation, search, entityId, AppEntityQueryKind.Search, [query], cancellationToken).ConfigureAwait(false);

            case "suggested":
                return await this.QueryEntities(invocation, search, entityId, AppEntityQueryKind.Suggested, [], cancellationToken).ConfigureAwait(false);

            default:
                return AppFunctionOutcome.Failed(AppFunctionErrorCode.NotFound, $"Unknown entity operation '{invocation.FunctionId}'");
        }
    }

    async Task<AppFunctionOutcome> QueryEntities(
        AppFunctionInvocation invocation,
        AppFunctionDescriptor function,
        string entityId,
        AppEntityQueryKind kind,
        IReadOnlyList<string> queryArguments,
        CancellationToken cancellationToken
    )
    {
        await using var scope = services.CreateAsyncScope();
        var context = new AppFunctionContext(function, invocation, scope.ServiceProvider);
        IReadOnlyList<AppEntityItem>? items = null;
        Exception? failure = null;
        var delegates = scope.ServiceProvider.GetServices<IAppFunctionDelegate>().ToList();

        try
        {
            // entity lookups expose app data, so they pass through the same delegates as functions
            var gate = await Gate(delegates, context, cancellationToken).ConfigureAwait(false);
            if (gate != null)
            {
                failure = Blocked(gate);
                return gate;
            }

            items = await registry.QueryEntities(entityId, kind, queryArguments, scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            var json = Write(w =>
            {
                w.WriteStartArray();
                foreach (var item in items)
                {
                    w.WriteStartObject();
                    w.WriteString("id", item.Id);
                    w.WriteString("title", item.Title);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            });
            return new AppFunctionOutcome(AppFunctionStatus.Success, json, context.Dialog);
        }
        catch (Exception ex)
        {
            failure = ex;
            return this.ToOutcome(function.Id, ex, cancellationToken);
        }
        finally
        {
            await this.Invoked(delegates, context, items, failure).ConfigureAwait(false);
        }
    }

    static async Task<AppFunctionOutcome?> Gate(List<IAppFunctionDelegate> delegates, AppFunctionContext context, CancellationToken cancellationToken)
    {
        foreach (var d in delegates)
        {
            var gate = await d.OnInvoking(context, cancellationToken).ConfigureAwait(false);
            switch (gate.Kind)
            {
                case AppFunctionGateKind.Deny:
                    return AppFunctionOutcome.Failed(AppFunctionErrorCode.Denied, gate.Message ?? "Not allowed");

                case AppFunctionGateKind.OpenApp when !context.IsForeground:
                    return context.Platform == AppFunctionPlatform.Apple
                        ? new AppFunctionOutcome(AppFunctionStatus.NeedsForeground, Message: gate.Message ?? "Continue in the app")
                        : AppFunctionOutcome.Failed(AppFunctionErrorCode.Denied, gate.Message ?? "Open the app to continue");
            }
        }
        return null;
    }

    /// <summary>What OnInvoked sees when a delegate stopped the call.</summary>
    static AppFunctionException Blocked(AppFunctionOutcome gate)
        => new(gate.ErrorCode ?? AppFunctionErrorCode.Denied, gate.Message ?? "Not allowed");

    async Task Invoked(List<IAppFunctionDelegate> delegates, AppFunctionContext context, object? result, Exception? failure)
    {
        foreach (var d in delegates)
        {
            try
            {
                await d.OnInvoked(context, result, failure).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "{Delegate}.OnInvoked failed for {FunctionId}", d.GetType().Name, context.FunctionId);
            }
        }
    }

    AppFunctionOutcome ToOutcome(string functionId, Exception ex, CancellationToken cancellationToken)
    {
        switch (ex)
        {
            case AppFunctionException afe:
                logger.LogInformation("App function {FunctionId} failed: {Code} {Message}", functionId, afe.Code, afe.Message);
                return AppFunctionOutcome.Failed(afe.Code, afe.Message);

            case OperationCanceledException when cancellationToken.IsCancellationRequested:
                return AppFunctionOutcome.Failed(AppFunctionErrorCode.Cancelled, "Cancelled");

            default:
                logger.LogError(ex, "App function {FunctionId} threw", functionId);
                return AppFunctionOutcome.Failed(AppFunctionErrorCode.AppError, ex.Message);
        }
    }

    static string Write(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            write(writer);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
