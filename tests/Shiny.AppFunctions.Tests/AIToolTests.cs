using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.AppFunctions.Extensions.AI;

namespace Shiny.AppFunctions.Tests;

public class AIToolTests
{
    static AppFunctionAITools Create(Action<IAppFunctionAIToolBuilder> configure, IAppFunctionDelegate? gate = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<AppFunctionDispatcher>>(NullLogger<AppFunctionDispatcher>.Instance);
        services.AddScoped<ScopedCounter>();
        services.AddScoped<IAppFunctionHandler<Greet, string>, GreetHandler>();
        services.AddScoped<IAppFunctionHandler<Fail>, FailHandler>();
        services.AddScoped<IAppEntityQuery<Customer>, CustomerQuery>();
        if (gate != null)
            services.AddSingleton(gate);
        services.AddAppFunctionsRuntime<EntityTestRegistry>();
        services.AddAppFunctionAITools(configure);
        return services.BuildServiceProvider().GetRequiredService<AppFunctionAITools>();
    }

    static AIFunction Tool(AppFunctionAITools tools, string name) => tools.Tools.OfType<AIFunction>().Single(x => x.Name == name);

    static async Task<JsonObject> Invoke(AIFunction tool, AIFunctionArguments arguments)
        => Assert.IsType<JsonObject>(await tool.InvokeAsync(arguments));

    static AIFunctionArguments Json(string json)
    {
        var args = new AIFunctionArguments();
        using var doc = JsonDocument.Parse(json);
        foreach (var p in doc.RootElement.EnumerateObject())
            args[p.Name] = p.Value.Clone();
        return args;
    }

    static string[] Names(AppFunctionAITools tools) => tools.Tools.Select(x => x.Name).ToArray();

    [Fact]
    public void AddAllFunctions_ExposesEveryFunctionInRegistryOrder()
        => Assert.Equal(["greet", "fail", "view_customer", "search_customer"], Names(Create(x => x.AddAllFunctions())));

    [Fact]
    public void AddFunction_ExposesOnlyThatFunction()
        => Assert.Equal(["greet"], Names(Create(x => x.AddFunction("greet"))));

    [Fact]
    public void EntityParameter_AddsItsSearchFunction()
        => Assert.Equal(["view_customer", "search_customer"], Names(Create(x => x.AddFunction("view_customer"))));

    [Fact]
    public void Exclude_WinsOverAllAndEntitySearch()
    {
        Assert.Equal(["greet", "view_customer", "search_customer"], Names(Create(x => x.AddAllFunctions().ExcludeFunction("fail"))));
        Assert.Equal(["view_customer"], Names(Create(x => x.AddFunction("view_customer").ExcludeFunction("search_customer"))));
    }

    [Fact]
    public void EmptyRegistration_ThrowsAtRegistration()
        => Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddAppFunctionAITools(_ => { }));

    [Fact]
    public void UnknownId_ThrowsOnResolve()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Create(x => x.AddFunction("nope")));
        Assert.Contains("nope", ex.Message);
    }

    [Fact]
    public void EverythingExcluded_ThrowsOnResolve()
        => Assert.Throws<InvalidOperationException>(() => Create(x => x.AddFunction("greet").ExcludeFunction("greet")));

    [Fact]
    public void Tool_UsesDescriptorNameDescriptionAndSchema()
    {
        var tool = Tool(Create(x => x.AddFunction("greet")), "greet");

        Assert.Equal("Says hi", tool.Description);
        Assert.Equal("object", tool.JsonSchema.GetProperty("type").GetString());
        Assert.Equal(["name", "times"], tool.JsonSchema.GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    [Fact]
    public async Task JsonElementArguments_RunTheHandler()
    {
        var result = await Invoke(Tool(Create(x => x.AddFunction("greet")), "greet"), Json("""{"name":"Bob","times":2}"""));

        Assert.True(result["success"]!.GetValue<bool>());
        Assert.Equal("hi Bob hi Bob", result["result"]!.GetValue<string>());
        Assert.Equal("said hi to Bob", result["message"]!.GetValue<string>());
    }

    [Fact]
    public async Task PrimitiveArguments_RunTheHandler()
    {
        var result = await Invoke(Tool(Create(x => x.AddFunction("greet")), "greet"), new AIFunctionArguments { ["name"] = "Ann", ["times"] = 1 });
        Assert.Equal("hi Ann", result["result"]!.GetValue<string>());
    }

    [Fact]
    public async Task VoidFunction_HasNoResult()
    {
        var result = await Invoke(Tool(Create(x => x.AddFunction("fail")), "fail"), new AIFunctionArguments { ["mode"] = "ok" });

        Assert.True(result["success"]!.GetValue<bool>());
        Assert.False(result.ContainsKey("result"));
    }

    [Fact]
    public async Task MissingArgument_ReturnsInvalidArgument()
    {
        var result = await Invoke(Tool(Create(x => x.AddFunction("greet")), "greet"), new AIFunctionArguments { ["name"] = "Bob" });

        Assert.Equal("InvalidArgument", result["code"]!.GetValue<string>());
        Assert.Equal("times is required", result["error"]!.GetValue<string>());
    }

    [Fact]
    public async Task HandlerException_ReturnsItsCodeAndMessage()
    {
        var result = await Invoke(Tool(Create(x => x.AddFunction("fail")), "fail"), new AIFunctionArguments { ["mode"] = "app" });

        Assert.Equal("Denied", result["code"]!.GetValue<string>());
        Assert.Equal("not today", result["error"]!.GetValue<string>());
    }

    [Fact]
    public async Task DelegateDeny_ReachesTheLlm()
    {
        var tools = Create(x => x.AddFunction("greet"), new Gate(AppFunctionGate.Deny("sign in first")));
        var result = await Invoke(Tool(tools, "greet"), new AIFunctionArguments { ["name"] = "Bob", ["times"] = 1 });

        Assert.Equal("Denied", result["code"]!.GetValue<string>());
        Assert.Equal("sign in first", result["error"]!.GetValue<string>());
    }

    [Fact]
    public async Task DelegateOpenApp_PassesBecauseTheChatIsInTheApp()
    {
        var tools = Create(x => x.AddFunction("greet"), new Gate(AppFunctionGate.OpenApp("Open the app to greet.")));
        var result = await Invoke(Tool(tools, "greet"), new AIFunctionArguments { ["name"] = "Bob", ["times"] = 1 });

        Assert.True(result["success"]!.GetValue<bool>());
    }

    [Fact]
    public async Task DelegateSeesOtherPlatformInTheForeground()
    {
        var gate = new Gate(AppFunctionGate.Allow);
        await Invoke(Tool(Create(x => x.AddFunction("greet"), gate), "greet"), new AIFunctionArguments { ["name"] = "Bob", ["times"] = 1 });

        Assert.Equal(AppFunctionPlatform.Other, gate.Platform);
        Assert.True(gate.IsForeground);
    }

    [Fact]
    public async Task SearchFunction_ReturnsIdsAndTitles()
    {
        var result = await Invoke(Tool(Create(x => x.AddFunction("search_customer")), "search_customer"), new AIFunctionArguments { ["query"] = "acme" });

        var items = result["result"]!.AsArray();
        Assert.Equal(["c1", "c3"], items.Select(x => x!["id"]!.GetValue<string>()).ToArray());
        Assert.Equal("Acme", items[0]!["title"]!.GetValue<string>());
    }

    class Gate(AppFunctionGate gate) : IAppFunctionDelegate
    {
        public AppFunctionPlatform? Platform;
        public bool IsForeground;

        public Task<AppFunctionGate> OnInvoking(AppFunctionContext context, CancellationToken cancellationToken)
        {
            this.Platform = context.Platform;
            this.IsForeground = context.IsForeground;
            return Task.FromResult(gate);
        }
    }
}

/// <summary><see cref="TestRegistry"/> plus a function that takes a customer entity, for the entity-search rules.</summary>
public class EntityTestRegistry : IAppFunctionRegistry
{
    readonly TestRegistry inner = new();

    public EntityTestRegistry()
    {
        var functions = this.inner.Functions.ToList();
        functions.Insert(2, new()
        {
            Id = "view_customer",
            Title = "View Customer",
            Description = "Shows a customer",
            Parameters = [new() { Name = "customer", Title = "Customer", Type = new() { Kind = AppValueKind.Entity, EntityId = "customer" }, IsRequired = true }],
            Result = AppTypeDescriptor.Void
        });
        this.Functions = functions;
    }

    public IReadOnlyList<AppFunctionDescriptor> Functions { get; }
    public IReadOnlyList<AppEntityDescriptor> Entities => this.inner.Entities;

    public ValueTask<object> CreateRequest(string functionId, JsonElement arguments, IServiceProvider services, CancellationToken cancellationToken)
        => this.inner.CreateRequest(functionId, arguments, services, cancellationToken);

    public Task<object?> Invoke(object request, AppFunctionContext context, CancellationToken cancellationToken)
        => this.inner.Invoke(request, context, cancellationToken);

    public void WriteResult(string functionId, object? result, Utf8JsonWriter writer)
        => this.inner.WriteResult(functionId, result, writer);

    public Task<IReadOnlyList<AppEntityItem>> QueryEntities(string entityId, AppEntityQueryKind kind, IReadOnlyList<string> arguments, IServiceProvider services, CancellationToken cancellationToken)
        => this.inner.QueryEntities(entityId, kind, arguments, services, cancellationToken);
}
