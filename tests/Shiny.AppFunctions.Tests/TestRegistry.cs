using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.AppFunctions.Tests;

// Hand-written stand-in for the generated registry: exercises the dispatcher without the source generator.
public record Greet(string Name, int Times) : IAppFunction<string>;
public record Fail(string Mode) : IAppFunction;
public record Customer(string Id, string Name);

public class GreetHandler(ScopedCounter counter) : IAppFunctionHandler<Greet, string>
{
    public Task<string> Handle(Greet request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        counter.Hits++;
        context.Say($"said hi to {request.Name}");
        return Task.FromResult(String.Join(" ", Enumerable.Repeat($"hi {request.Name}", request.Times)));
    }
}

public class FailHandler : IAppFunctionHandler<Fail>
{
    public async Task Handle(Fail request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        switch (request.Mode)
        {
            case "app": throw new AppFunctionException(AppFunctionErrorCode.Denied, "not today");
            case "boom": throw new InvalidOperationException("boom");
            case "slow": await Task.Delay(5000, cancellationToken); break;
        }
    }
}

public class CustomerQuery : IAppEntityQuery<Customer>
{
    public static readonly Customer[] All = [new("c1", "Acme"), new("c2", "Globex"), new("c3", "Acme West")];

    public Task<IReadOnlyList<Customer>> GetByIds(IReadOnlyList<string> ids, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Customer>>(All.Where(x => ids.Contains(x.Id)).ToList());

    public Task<IReadOnlyList<Customer>> Search(string text, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Customer>>(All.Where(x => x.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList());
}

public class ScopedCounter : IDisposable
{
    public static int Disposed;
    public int Hits;
    public void Dispose() => Interlocked.Increment(ref Disposed);
}

public class TestRegistry : IAppFunctionRegistry
{
    public IReadOnlyList<AppFunctionDescriptor> Functions { get; } =
    [
        new()
        {
            Id = "greet",
            Title = "Greet",
            Description = "Says hi",
            Parameters =
            [
                new() { Name = "name", Title = "Name", Type = new() { Kind = AppValueKind.String }, IsRequired = true },
                new() { Name = "times", Title = "Times", Type = new() { Kind = AppValueKind.Int32 }, IsRequired = true }
            ],
            Result = new() { Kind = AppValueKind.String }
        },
        new()
        {
            Id = "fail",
            Title = "Fail",
            Description = "Fails",
            Parameters = [new() { Name = "mode", Title = "Mode", Type = new() { Kind = AppValueKind.String }, IsRequired = true }],
            Result = AppTypeDescriptor.Void
        },
        new()
        {
            Id = "search_customer",
            Title = "Find Customer",
            Description = "Finds customers",
            SearchesEntityId = "customer",
            Parameters = [new() { Name = "query", Title = "Query", Type = new() { Kind = AppValueKind.String }, IsRequired = true }],
            Result = new() { Kind = AppValueKind.Array, ItemType = new() { Kind = AppValueKind.Object } }
        }
    ];

    public IReadOnlyList<AppEntityDescriptor> Entities { get; } = [new() { Id = "customer", Title = "Customer", TypeName = "Customer" }];

    public ValueTask<object> CreateRequest(string functionId, JsonElement arguments, IServiceProvider services, CancellationToken cancellationToken) => functionId switch
    {
        "greet" => ValueTask.FromResult<object>(new Greet(
            AppFunctionArguments.GetString(arguments, "name", true)!,
            AppFunctionArguments.GetInt32(arguments, "times", true)!.Value
        )),
        "fail" => ValueTask.FromResult<object>(new Fail(AppFunctionArguments.GetString(arguments, "mode", true)!)),
        _ => throw new AppFunctionException(AppFunctionErrorCode.NotFound, functionId)
    };

    public async Task<object?> Invoke(object request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        switch (request)
        {
            case Greet g:
                return await context.Services.GetRequiredService<IAppFunctionHandler<Greet, string>>().Handle(g, context, cancellationToken);
            case Fail f:
                await context.Services.GetRequiredService<IAppFunctionHandler<Fail>>().Handle(f, context, cancellationToken);
                return null;
        }
        throw new InvalidOperationException();
    }

    public void WriteResult(string functionId, object? result, Utf8JsonWriter writer) => writer.WriteStringValue((string)result!);

    public async Task<IReadOnlyList<AppEntityItem>> QueryEntities(string entityId, AppEntityQueryKind kind, IReadOnlyList<string> arguments, IServiceProvider services, CancellationToken cancellationToken)
    {
        var query = services.GetRequiredService<IAppEntityQuery<Customer>>();
        var found = kind switch
        {
            AppEntityQueryKind.ByIds => await query.GetByIds(arguments, cancellationToken),
            AppEntityQueryKind.Search => await query.Search(arguments[0], cancellationToken),
            _ => await query.Suggested(cancellationToken)
        };
        return found.Select(x => new AppEntityItem(x.Id, x.Name)).ToList();
    }
}
