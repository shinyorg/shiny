using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shiny.AppFunctions.Tests;

public class DispatcherTests
{
    readonly List<string> log = [];

    AppFunctionDispatcher Create(params IAppFunctionDelegate[] delegates) => this.Create<TestRegistry>(delegates);

    AppFunctionDispatcher Create<TRegistry>(params IAppFunctionDelegate[] delegates) where TRegistry : class, IAppFunctionRegistry
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<AppFunctionDispatcher>>(NullLogger<AppFunctionDispatcher>.Instance);
        services.AddScoped<ScopedCounter>();
        services.AddScoped<IAppFunctionHandler<Greet, string>, GreetHandler>();
        services.AddScoped<IAppFunctionHandler<Fail>, FailHandler>();
        services.AddScoped<IAppEntityQuery<Customer>, CustomerQuery>();
        foreach (var d in delegates)
            services.AddSingleton(d);
        services.AddAppFunctionsRuntime<TRegistry>();
        return services.BuildServiceProvider().GetRequiredService<AppFunctionDispatcher>();
    }

    static AppFunctionInvocation Call(string id, AppFunctionPlatform platform = AppFunctionPlatform.Other, bool foreground = false) => new(id, platform, foreground);

    [Fact]
    public async Task Success_ReturnsJsonValueAndDialog()
    {
        var outcome = await this.Create().Execute(Call("greet"), """{"name":"Bob","times":2}""", CancellationToken.None);

        Assert.Equal(AppFunctionStatus.Success, outcome.Status);
        Assert.Equal("\"hi Bob hi Bob\"", outcome.ResultJson);
        Assert.Equal("said hi to Bob", outcome.Dialog);
    }

    [Fact]
    public async Task NumbersAsStrings_AreAccepted()
    {
        var outcome = await this.Create().Execute(Call("greet"), """{"name":"Bob","times":"1"}""", CancellationToken.None);
        Assert.Equal("\"hi Bob\"", outcome.ResultJson);
    }

    [Theory]
    [InlineData("""{"times":1}""", "name is required")]
    [InlineData("""{"name":"Bob"}""", "times is required")]
    [InlineData("""{"name":"Bob","times":1.5}""", "times must be a whole number")]
    [InlineData("""{"name":null,"times":1}""", "name is required")]
    public async Task BadArguments_AreInvalidArgument(string json, string message)
    {
        var outcome = await this.Create().Execute(Call("greet"), json, CancellationToken.None);

        Assert.Equal(AppFunctionStatus.Error, outcome.Status);
        Assert.Equal(AppFunctionErrorCode.InvalidArgument, outcome.ErrorCode);
        Assert.Equal(message, outcome.Message);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    public async Task MalformedJson_IsInvalidArgument(string json)
    {
        var outcome = await this.Create().Execute(Call("greet"), json, CancellationToken.None);
        Assert.Equal(AppFunctionErrorCode.InvalidArgument, outcome.ErrorCode);
    }

    [Fact]
    public async Task UnknownFunction_IsNotFound()
    {
        var outcome = await this.Create().Execute(Call("nope"), "{}", CancellationToken.None);
        Assert.Equal(AppFunctionErrorCode.NotFound, outcome.ErrorCode);
    }

    [Fact]
    public async Task VoidFunction_HasNoResultJson()
    {
        var outcome = await this.Create().Execute(Call("fail"), """{"mode":"ok"}""", CancellationToken.None);

        Assert.Equal(AppFunctionStatus.Success, outcome.Status);
        Assert.Null(outcome.ResultJson);
    }

    [Fact]
    public async Task AppFunctionException_KeepsItsCode()
    {
        var outcome = await this.Create().Execute(Call("fail"), """{"mode":"app"}""", CancellationToken.None);

        Assert.Equal(AppFunctionErrorCode.Denied, outcome.ErrorCode);
        Assert.Equal("not today", outcome.Message);
    }

    [Fact]
    public async Task OtherExceptions_AreAppError()
    {
        var outcome = await this.Create().Execute(Call("fail"), """{"mode":"boom"}""", CancellationToken.None);

        Assert.Equal(AppFunctionErrorCode.AppError, outcome.ErrorCode);
        Assert.Equal("boom", outcome.Message);
    }

    [Fact]
    public async Task Cancellation_IsCancelled()
    {
        using var cts = new CancellationTokenSource(100);
        var outcome = await this.Create().Execute(Call("fail"), """{"mode":"slow"}""", cts.Token);
        Assert.Equal(AppFunctionErrorCode.Cancelled, outcome.ErrorCode);
    }

    [Fact]
    public async Task Delegates_RunInOrder_AroundTheHandler()
    {
        var a = new RecordingDelegate("a", this.log);
        var b = new RecordingDelegate("b", this.log);
        await this.Create(a, b).Execute(Call("greet"), """{"name":"Bob","times":1}""", CancellationToken.None);

        Assert.Equal(["a:invoking greet Greet", "b:invoking greet Greet", "a:invoked hi Bob ", "b:invoked hi Bob "], this.log);
    }

    [Fact]
    public async Task Deny_StopsTheHandler()
    {
        var deny = new RecordingDelegate("d", this.log) { Gate = AppFunctionGate.Deny("sign in first") };
        var later = new RecordingDelegate("later", this.log);
        var outcome = await this.Create(deny, later).Execute(Call("greet"), """{"name":"Bob","times":1}""", CancellationToken.None);

        Assert.Equal(AppFunctionErrorCode.Denied, outcome.ErrorCode);
        Assert.Equal("sign in first", outcome.Message);
        Assert.DoesNotContain("later:invoking greet Greet", this.log);
        Assert.Contains("d:invoked  sign in first", this.log); // OnInvoked still runs, with the refusal
    }

    [Fact]
    public async Task OpenApp_OnApple_NeedsForeground()
    {
        var gate = new RecordingDelegate("g", this.log) { Gate = AppFunctionGate.OpenApp("continue in the app") };
        var outcome = await this.Create(gate).Execute(Call("greet", AppFunctionPlatform.Apple), """{"name":"Bob","times":1}""", CancellationToken.None);

        Assert.Equal(AppFunctionStatus.NeedsForeground, outcome.Status);
        Assert.Equal("continue in the app", outcome.Message);
    }

    [Fact]
    public async Task OpenApp_WhenAlreadyForeground_Runs()
    {
        var gate = new RecordingDelegate("g", this.log) { Gate = AppFunctionGate.OpenApp("continue in the app") };
        var outcome = await this.Create(gate).Execute(Call("greet", AppFunctionPlatform.Apple, foreground: true), """{"name":"Bob","times":1}""", CancellationToken.None);

        Assert.Equal(AppFunctionStatus.Success, outcome.Status);
    }

    [Fact]
    public async Task OpenApp_OnAndroid_IsDenied()
    {
        var gate = new RecordingDelegate("g", this.log) { Gate = AppFunctionGate.OpenApp("open the app") };
        var outcome = await this.Create(gate).Execute(Call("greet", AppFunctionPlatform.Android), """{"name":"Bob","times":1}""", CancellationToken.None);

        Assert.Equal(AppFunctionErrorCode.Denied, outcome.ErrorCode);
        Assert.Equal("open the app", outcome.Message);
    }

    [Theory]
    [InlineData(AppFunctionPlatform.Android)]
    [InlineData(AppFunctionPlatform.Other)]
    public async Task OpensApp_InTheBackground_IsDeniedBeforeDelegates(AppFunctionPlatform platform)
    {
        var d = new RecordingDelegate("d", this.log);
        var outcome = await this.Create<OpensAppTestRegistry>(d).Execute(Call("greet", platform), """{"name":"Bob","times":1}""", CancellationToken.None);

        Assert.Equal(AppFunctionErrorCode.Denied, outcome.ErrorCode);
        Assert.Equal("Open the app to continue", outcome.Message);
        Assert.DoesNotContain(this.log, x => x.StartsWith("d:invoking"));
        Assert.Contains(this.log, x => x.StartsWith("d:invoked"));
    }

    [Fact]
    public async Task OpensApp_OnAppleInTheBackground_NeedsForeground()
    {
        var outcome = await this.Create<OpensAppTestRegistry>().Execute(Call("greet", AppFunctionPlatform.Apple), """{"name":"Bob","times":1}""", CancellationToken.None);
        Assert.Equal(AppFunctionStatus.NeedsForeground, outcome.Status);
    }

    [Theory]
    [InlineData(AppFunctionPlatform.Apple)]
    [InlineData(AppFunctionPlatform.Android)]
    [InlineData(AppFunctionPlatform.Other)]
    public async Task OpensApp_InTheForeground_Runs(AppFunctionPlatform platform)
    {
        var outcome = await this.Create<OpensAppTestRegistry>().Execute(Call("greet", platform, foreground: true), """{"name":"Bob","times":1}""", CancellationToken.None);
        Assert.Equal(AppFunctionStatus.Success, outcome.Status);
    }

    [Fact]
    public async Task ThrowingOnInvoked_DoesNotChangeTheOutcome()
    {
        var bad = new RecordingDelegate("bad", this.log) { ThrowOnInvoked = true };
        var outcome = await this.Create(bad).Execute(Call("greet"), """{"name":"Bob","times":1}""", CancellationToken.None);
        Assert.Equal(AppFunctionStatus.Success, outcome.Status);
    }

    [Fact]
    public async Task EachInvocation_GetsItsOwnScope()
    {
        var before = ScopedCounter.Disposed;
        var dispatcher = this.Create();
        await dispatcher.Execute(Call("greet"), """{"name":"a","times":1}""", CancellationToken.None);
        await dispatcher.Execute(Call("greet"), """{"name":"b","times":1}""", CancellationToken.None);

        Assert.True(ScopedCounter.Disposed - before >= 2);
    }

    [Fact]
    public async Task SearchFunction_RunsTheEntityQuery()
    {
        var outcome = await this.Create().Execute(Call("search_customer"), """{"query":"acme"}""", CancellationToken.None);

        Assert.Equal(AppFunctionStatus.Success, outcome.Status);
        Assert.Equal("""[{"id":"c1","title":"Acme"},{"id":"c3","title":"Acme West"}]""", outcome.ResultJson);
    }

    [Fact]
    public async Task EntityOperations_ForAppleQueries()
    {
        var dispatcher = this.Create();

        var byIds = await dispatcher.Execute(Call("entity:customer:ids"), """{"ids":["c2","missing"]}""", CancellationToken.None);
        Assert.Equal("""[{"id":"c2","title":"Globex"}]""", byIds.ResultJson);

        var search = await dispatcher.Execute(Call("entity:customer:search"), """{"query":"west"}""", CancellationToken.None);
        Assert.Equal("""[{"id":"c3","title":"Acme West"}]""", search.ResultJson);

        var suggested = await dispatcher.Execute(Call("entity:customer:suggested"), "{}", CancellationToken.None);
        Assert.Equal("[]", suggested.ResultJson);

        var unknown = await dispatcher.Execute(Call("entity:nope:ids"), "{}", CancellationToken.None);
        Assert.Equal(AppFunctionErrorCode.NotFound, unknown.ErrorCode);
    }

    [Fact]
    public async Task EntityQueries_PassThroughDelegates()
    {
        var deny = new RecordingDelegate("d", this.log) { Gate = AppFunctionGate.Deny("no") };
        var outcome = await this.Create(deny).Execute(Call("entity:customer:search"), """{"query":"a"}""", CancellationToken.None);
        Assert.Equal(AppFunctionErrorCode.Denied, outcome.ErrorCode);
    }

    [Fact]
    public async Task HostReady_ReleasesWaiters()
    {
        AppFunctionsHost.Reset();
        var waiting = AppFunctionsHost.WaitForDispatcher();
        Assert.False(waiting.IsCompleted);

        var dispatcher = this.Create();
        AppFunctionsHost.SetReady(dispatcher);
        Assert.Same(dispatcher, await waiting);
    }

    [Fact]
    public void ParametersJsonSchema_DescribesRequiredParameters()
    {
        var schema = JsonDocument.Parse(new TestRegistry().Functions[0].GetParametersJsonSchema()).RootElement;

        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.Equal("string", schema.GetProperty("properties").GetProperty("name").GetProperty("type").GetString());
        Assert.Equal("integer", schema.GetProperty("properties").GetProperty("times").GetProperty("type").GetString());
        Assert.Equal(["name", "times"], schema.GetProperty("required").EnumerateArray().Select(x => x.GetString()));
    }

    class RecordingDelegate(string name, List<string> log) : IAppFunctionDelegate
    {
        public AppFunctionGate Gate { get; init; } = AppFunctionGate.Allow;
        public bool ThrowOnInvoked { get; init; }

        public Task<AppFunctionGate> OnInvoking(AppFunctionContext context, CancellationToken cancellationToken)
        {
            log.Add($"{name}:invoking {context.FunctionId} {context.Request?.GetType().Name}");
            return Task.FromResult(this.Gate);
        }

        public Task OnInvoked(AppFunctionContext context, object? result, Exception? exception)
        {
            if (this.ThrowOnInvoked)
                throw new InvalidOperationException("delegate bug");
            log.Add($"{name}:invoked {result} {exception?.Message}");
            return Task.CompletedTask;
        }
    }
}
