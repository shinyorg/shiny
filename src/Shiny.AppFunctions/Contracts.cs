namespace Shiny.AppFunctions;

/// <summary>An app function request with no result. Mark the record with <see cref="AppFunctionAttribute"/>.</summary>
public interface IAppFunction;

/// <summary>An app function request that returns <typeparamref name="TResult"/>. Mark the record with <see cref="AppFunctionAttribute"/>.</summary>
public interface IAppFunction<TResult>;

/// <summary>Runs an app function that has no result. Resolved from a new DI scope per invocation.</summary>
public interface IAppFunctionHandler<in TRequest> where TRequest : IAppFunction
{
    Task Handle(TRequest request, AppFunctionContext context, CancellationToken cancellationToken);
}

/// <summary>Runs an app function. Resolved from a new DI scope per invocation.</summary>
public interface IAppFunctionHandler<in TRequest, TResult> where TRequest : IAppFunction<TResult>
{
    Task<TResult> Handle(TRequest request, AppFunctionContext context, CancellationToken cancellationToken);
}

/// <summary>Looks up <see cref="AppEntityAttribute"/> entities for the assistant. Resolved from a new DI scope per call.</summary>
public interface IAppEntityQuery<TEntity>
{
    /// <summary>Returns the entities with these ids (ids that no longer exist are simply left out).</summary>
    Task<IReadOnlyList<TEntity>> GetByIds(IReadOnlyList<string> ids, CancellationToken cancellationToken);

    /// <summary>Free-text search, for example as the user types in Shortcuts or when an agent looks one up.</summary>
    Task<IReadOnlyList<TEntity>> Search(string text, CancellationToken cancellationToken);

    /// <summary>Entities offered before the user types anything. Empty by default.</summary>
    Task<IReadOnlyList<TEntity>> Suggested(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<TEntity>>([]);
}

/// <summary>
/// Cross-cutting hooks around every app function and entity query: authorization, feature flags, telemetry.
/// Register as many as you like; they run in registration order and are resolved from the invocation's scope.
/// </summary>
public interface IAppFunctionDelegate
{
    /// <summary>Runs before the handler. The first delegate that does not return <see cref="AppFunctionGate.Allow"/> wins.</summary>
    Task<AppFunctionGate> OnInvoking(AppFunctionContext context, CancellationToken cancellationToken) => Task.FromResult(AppFunctionGate.Allow);

    /// <summary>Runs after the handler, with its result or the exception it threw. Exceptions thrown here are logged and ignored.</summary>
    Task OnInvoked(AppFunctionContext context, object? result, Exception? exception) => Task.CompletedTask;
}

public enum AppFunctionGateKind
{
    Allow,
    Deny,
    OpenApp
}

/// <summary>The decision returned by <see cref="IAppFunctionDelegate.OnInvoking"/>.</summary>
public sealed class AppFunctionGate
{
    AppFunctionGate(AppFunctionGateKind kind, string? message)
    {
        this.Kind = kind;
        this.Message = message;
    }

    public AppFunctionGateKind Kind { get; }
    public string? Message { get; }

    public static AppFunctionGate Allow { get; } = new(AppFunctionGateKind.Allow, null);

    /// <summary>Refuses the call. The message is shown or spoken to the user.</summary>
    public static AppFunctionGate Deny(string message) => new(AppFunctionGateKind.Deny, message);

    /// <summary>
    /// iOS: asks the user to continue in the app, then runs the handler again in the foreground
    /// (<see cref="AppFunctionContext.IsForeground"/> is true). Android: refuses the call with the message.
    /// </summary>
    public static AppFunctionGate OpenApp(string message) => new(AppFunctionGateKind.OpenApp, message);
}
