using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sample.Orders;
using Shiny.AppFunctions.Generated;

namespace Shiny.AppFunctions.SourceGenerators.Tests;

/// <summary>Runs the code the real generator produced for SampleModel.cs.</summary>
public class GeneratedRegistryTests
{
    static AppFunctionDispatcher Dispatcher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppFunctions(); // generated
        return services.BuildServiceProvider().GetRequiredService<AppFunctionDispatcher>();
    }

    static Task<AppFunctionOutcome> Run(string id, string json, AppFunctionPlatform platform = AppFunctionPlatform.Other)
        => Dispatcher().Execute(new AppFunctionInvocation(id, platform), json, CancellationToken.None);

    [Fact]
    public void AddAppFunctions_RegistersEverything()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppFunctions();
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        Assert.IsType<CreateOrderHandler>(scope.ServiceProvider.GetRequiredService<IAppFunctionHandler<CreateOrder, OrderResult>>());
        Assert.IsType<CountOrdersHandler>(scope.ServiceProvider.GetRequiredService<IAppFunctionHandler<CountOrders, int>>());
        Assert.IsType<CancelOrderHandler>(scope.ServiceProvider.GetRequiredService<IAppFunctionHandler<CancelOrder>>());
        Assert.IsType<CustomerQuery>(scope.ServiceProvider.GetRequiredService<IAppEntityQuery<Customer>>());
        Assert.Contains(scope.ServiceProvider.GetServices<IAppFunctionDelegate>(), x => x is AuditDelegate);
        Assert.IsType<GeneratedAppFunctionRegistry>(sp.GetRequiredService<IAppFunctionRegistry>());
        Assert.Contains(sp.GetServices<Shiny.IShinyStartupTask>(), x => x.GetType().Name == "AppFunctionsStartupTask");
    }

    [Fact]
    public void Descriptors_DescribeTheModel()
    {
        var registry = new GeneratedAppFunctionRegistry();
        Assert.Equal(["create_order", "count_orders", "cancel_order", "search_customer"], registry.Functions.Select(x => x.Id));

        var create = registry.Functions[0];
        Assert.Equal("Create Order", create.Title);
        Assert.Equal("Creates an order for a customer", create.Description);
        Assert.Equal(["customer", "quantity", "priority", "due", "note", "rush"], create.Parameters.Select(x => x.Name));
        Assert.Equal([true, true, true, false, false, false], create.Parameters.Select(x => x.IsRequired));
        Assert.Equal("Who the order is for", create.Parameters[0].Description);
        Assert.Equal(AppValueKind.Entity, create.Parameters[0].Type.Kind);
        Assert.Equal("customer", create.Parameters[0].Type.EntityId);
        Assert.Equal(["Low", "Normal", "High"], create.Parameters[2].Type.EnumValues);
        Assert.Equal(AppValueKind.Object, create.Result.Kind);
        Assert.Equal(AppValueKind.Array, create.Result.Properties.Single(p => p.Name == "lines").Type.Kind);

        Assert.True(registry.Functions[2].OpensApp);
        Assert.Equal(["number"], registry.Functions[2].Parameters.Select(x => x.Name));
        Assert.True(registry.Functions[2].Parameters[0].IsRequired); // C# `required`

        Assert.Equal("customer", registry.Functions[3].SearchesEntityId);
        Assert.Equal("customer", registry.Entities.Single().Id);
    }

    [Fact]
    public async Task CreateOrder_BindsEveryKindOfArgument_AndWritesTheResult()
    {
        var outcome = await Run("create_order", """{"customer":"c2","quantity":3,"priority":"high","due":"2026-12-24T10:30:00.000-05:00","note":"gift","rush":false}""");

        Assert.Equal(AppFunctionStatus.Success, outcome.Status);
        Assert.Equal("Order for Globex created", outcome.Dialog);
        var r = JsonDocument.Parse(outcome.ResultJson!).RootElement;
        Assert.Equal("ORD-1", r.GetProperty("number").GetString());
        Assert.Equal(29.97, r.GetProperty("total").GetDouble(), 3);
        Assert.Equal("High", r.GetProperty("priority").GetString());
        Assert.Equal("2026-12-24T10:30:00.000-05:00", r.GetProperty("due").GetString());
        Assert.Equal("SKU-3", r.GetProperty("lines")[0].GetProperty("sku").GetString());
        Assert.Equal(3, r.GetProperty("lines")[0].GetProperty("quantity").GetInt32());
        Assert.Equal("gift", r.GetProperty("note").GetString());
    }

    [Fact]
    public async Task OptionalArguments_CanBeLeftOut()
    {
        var outcome = await Run("create_order", """{"customer":"c1","quantity":1,"priority":"Low","rush":true}""");

        Assert.Equal(AppFunctionStatus.Success, outcome.Status);
        var r = JsonDocument.Parse(outcome.ResultJson!).RootElement;
        Assert.Equal("High", r.GetProperty("priority").GetString()); // rush
        Assert.Equal(JsonValueKind.Null, r.GetProperty("note").ValueKind);
    }

    [Theory]
    [InlineData("""{"quantity":1,"priority":"Low"}""", AppFunctionErrorCode.InvalidArgument, "customer is required")]
    [InlineData("""{"customer":"zzz","quantity":1,"priority":"Low"}""", AppFunctionErrorCode.NotFound, "'zzz' was not found")]
    [InlineData("""{"customer":"c1","quantity":1,"priority":"Urgent"}""", AppFunctionErrorCode.InvalidArgument, "priority must be one of: Low, Normal, High")]
    [InlineData("""{"customer":"c1","quantity":1,"priority":"Low","due":"soon"}""", AppFunctionErrorCode.InvalidArgument, "due must be an ISO 8601 date")]
    public async Task BadArguments_AreReported(string json, AppFunctionErrorCode code, string message)
    {
        var outcome = await Run("create_order", json);
        Assert.Equal(code, outcome.ErrorCode);
        Assert.Equal(message, outcome.Message);
    }

    [Fact]
    public async Task PrimitiveResult()
    {
        var outcome = await Run("count_orders", "{}");
        Assert.Equal("42", outcome.ResultJson);
    }

    [Fact]
    public async Task VoidFunction_WithRequiredInitializerProperty()
    {
        var outcome = await Run("cancel_order", """{"number":"ORD-9"}""");
        Assert.Equal(AppFunctionStatus.Success, outcome.Status);
        Assert.Null(outcome.ResultJson);
        Assert.Equal("ORD-9", CancelOrderHandler.LastCancelled);

        var missing = await Run("cancel_order", "{}");
        Assert.Equal("number is required", missing.Message);
    }

    [Fact]
    public async Task GeneratedSearch_AndEntityOperations()
    {
        var search = await Run("search_customer", """{"query":"glo"}""");
        Assert.Equal("""[{"id":"c2","title":"Globex"}]""", search.ResultJson);

        var suggested = await Run("entity:customer:suggested", "{}");
        Assert.Equal("""[{"id":"c1","title":"Acme"}]""", suggested.ResultJson);
    }

    [Fact]
    public async Task GeneratedDelegate_RunsForEveryCall()
    {
        await Run("count_orders", "{}");
        lock (AuditDelegate.Calls)
            Assert.Contains("count_orders", AuditDelegate.Calls);
    }

    [Fact]
    public void AndroidSchema_IsTheJetpackLayout()
    {
        var v1 = XDocument.Parse(__ShinyAppFunctionsManifest.AndroidFunctions);
        Assert.Equal(["create_order", "count_orders", "cancel_order", "search_customer"], v1.Descendants("function_id").Select(x => x.Value));

        var v2 = XDocument.Parse(__ShinyAppFunctionsManifest.AndroidFunctionsV2);
        var create = v2.Root!.Elements("appfunction").Single(x => x.Element("id")!.Value == "create_order");
        var parameters = create.Elements("parameters").ToDictionary(x => x.Element("name")!.Value, x => x.Element("dataTypeMetadata")!.Element("type")!.Value);
        Assert.Equal(new Dictionary<string, string>
        {
            ["customer"] = "8", ["quantity"] = "7", ["priority"] = "8", ["due"] = "8", ["note"] = "8", ["rush"] = "1"
        }, parameters);
        Assert.Equal(["Low", "Normal", "High"], create.Elements("parameters").Single(x => x.Element("name")!.Value == "priority").Descendants("enumValues").Select(x => x.Value));
        Assert.Equal("11", create.Element("response")!.Element("valueType")!.Element("type")!.Value);
        Assert.Equal("Sample.Orders.OrderResult", create.Element("response")!.Element("valueType")!.Element("dataTypeReference")!.Value);

        // components: the result object and the nested line object, each once
        var components = v2.Root.Element("AppFunctionComponentMetadataDocument")!.Elements("dataTypes").Select(x => x.Element("name")!.Value).ToList();
        Assert.Equal(["Sample.Orders.OrderLine", "Sample.Orders.OrderResult", "Shiny.AppFunctions.AppEntityItem"], components.Order());

        var count = v2.Root.Elements("appfunction").Single(x => x.Element("id")!.Value == "count_orders");
        Assert.Equal("7", count.Element("response")!.Element("valueType")!.Element("type")!.Value);
        var cancel = v2.Root.Elements("appfunction").Single(x => x.Element("id")!.Value == "cancel_order");
        Assert.Equal("0", cancel.Element("response")!.Element("valueType")!.Element("type")!.Value);
    }

    [Fact]
    public void Swift_ContainsIntentsEntitiesEnumsAndShortcuts()
    {
        var swift = __ShinyAppFunctionsManifest.Swift;
        Assert.Contains("struct CreateOrderIntent: AppIntent {", swift);
        Assert.Contains("static let openAppWhenRun: Bool = true", swift); // cancel_order
        Assert.Contains("var customer: CustomerAppEntity", swift);
        Assert.Contains("var due: Date?", swift);
        Assert.Contains("enum PriorityAppEnum: String, AppEnum {", swift);
        Assert.Contains("struct CustomerAppEntityQuery: EntityStringQuery {", swift);
        Assert.Contains("phrases: [\"Create an order in \\(.applicationName)\", \"Start a \\(.applicationName) order\"]", swift);
        Assert.Contains("shortTitle: \"New Order\"", swift);
        Assert.DoesNotContain("search_customer", swift); // Android-only
    }

    /// <summary>Typechecks the generated Swift together with the runtime, with the real iOS SDK. Skipped without Xcode.</summary>
    [Fact]
    public void Swift_Typechecks()
    {
        if (!OperatingSystem.IsMacOS() || !File.Exists("/usr/bin/xcrun"))
            return;

        var dir = Directory.CreateTempSubdirectory("shinyaf-swift").FullName;
        var generated = Path.Combine(dir, "Generated.swift");
        File.WriteAllText(generated, __ShinyAppFunctionsManifest.Swift);
        var runtime = Path.Combine(AppContext.BaseDirectory, "swift", "ShinyAppFunctions.swift");

        var sdk = RunTool("xcrun", "--sdk iphonesimulator --show-sdk-path").Trim();
        var output = RunTool("xcrun", $"--sdk iphonesimulator swiftc -typecheck -parse-as-library -swift-version 5 -target arm64-apple-ios16.0-simulator -sdk \"{sdk}\" \"{runtime}\" \"{generated}\"", expectSuccess: false);
        Assert.True(!output.Contains("error:"), output);
    }

    static string RunTool(string file, string args, bool expectSuccess = true)
    {
        var psi = new ProcessStartInfo(file, args) { RedirectStandardOutput = true, RedirectStandardError = true };
        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (expectSuccess && p.ExitCode != 0)
            throw new InvalidOperationException(stderr);
        return stdout + stderr;
    }
}
