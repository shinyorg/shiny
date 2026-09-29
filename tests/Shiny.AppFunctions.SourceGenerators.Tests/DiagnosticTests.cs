using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shiny.AppFunctions.SourceGenerators.Tests;

public class DiagnosticTests
{
    static (ImmutableArray<Diagnostic> Diagnostics, string? Generated) Run(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(x => MetadataReference.CreateFromFile(x))
            .Append(MetadataReference.CreateFromFile(typeof(AppFunctionAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "Test",
            [CSharpSyntaxTree.ParseText("global using System; global using System.Threading; global using System.Threading.Tasks; global using System.Collections.Generic; global using Shiny.AppFunctions;\n" + source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );

        var driver = CSharpGeneratorDriver.Create(new AppFunctionsGenerator()).RunGenerators(compilation);
        var result = driver.GetRunResult();
        return (result.Diagnostics, result.GeneratedTrees.FirstOrDefault()?.ToString());
    }

    static void AssertDiagnostic(string source, string id, string? messageContains = null)
    {
        var (diagnostics, generated) = Run(source);
        var d = Assert.Single(diagnostics, x => x.Id == id);
        if (messageContains != null)
            Assert.Contains(messageContains, d.GetMessage());
        Assert.Null(generated); // errors stop generation
    }

    const string Handler = "public class H : IAppFunctionHandler<F, string> { public Task<string> Handle(F r, AppFunctionContext c, CancellationToken t) => Task.FromResult(\"\"); }";

    [Fact]
    public void ValidModel_GeneratesWithoutDiagnostics()
    {
        var (diagnostics, generated) = Run($"[AppFunction(\"do_it\")] public record F(string Name) : IAppFunction<string>; {Handler}");
        Assert.Empty(diagnostics);
        Assert.Contains("AddAppFunctions", generated);
    }

    [Theory]
    [InlineData("DoIt")]
    [InlineData("1abc")]
    [InlineData("do-it")]
    public void InvalidId(string id)
        => AssertDiagnostic($"[AppFunction(\"{id}\")] public record F(string Name) : IAppFunction<string>; {Handler}", "SHAF001", id);

    [Fact]
    public void MissingInterface()
        => AssertDiagnostic("[AppFunction(\"f\")] public record F(string Name);", "SHAF002");

    [Fact]
    public void UnsupportedParameterType()
        => AssertDiagnostic($"[AppFunction(\"f\")] public record F(Guid Id) : IAppFunction<string>; {Handler}", "SHAF003", "Guid");

    [Fact]
    public void UnsupportedResultType()
        => AssertDiagnostic("[AppFunction(\"f\")] public record F(string Name) : IAppFunction<Dictionary<string, object>>; public class H : IAppFunctionHandler<F, Dictionary<string, object>> { public Task<Dictionary<string, object>> Handle(F r, AppFunctionContext c, CancellationToken t) => null!; }", "SHAF003");

    [Fact]
    public void NoConstructor()
        => AssertDiagnostic($"[AppFunction(\"f\")] public class F : IAppFunction<string> {{ F() {{ }} public string Name {{ get; set; }} = \"\"; }} {Handler}", "SHAF004");

    [Fact]
    public void NoHandler()
        => AssertDiagnostic("[AppFunction(\"f\")] public record F(string Name) : IAppFunction<string>;", "SHAF005", "'f'");

    [Fact]
    public void MultipleHandlers()
        => AssertDiagnostic($"[AppFunction(\"f\")] public record F(string Name) : IAppFunction<string>; {Handler} public class H2 : H {{ }}", "SHAF006", "H, H2");

    [Fact]
    public void EntityNeedsId()
        => AssertDiagnostic("[AppEntity(\"thing\")] public record Thing(string Key, string Name);", "SHAF007");

    [Fact]
    public void EntityDisplayPropertyMissing()
        => AssertDiagnostic("[AppEntity(\"thing\", DisplayProperty = \"Label\")] public record Thing(string Id); public class Q : IAppEntityQuery<Thing> { public Task<IReadOnlyList<Thing>> GetByIds(IReadOnlyList<string> ids, CancellationToken t) => null!; public Task<IReadOnlyList<Thing>> Search(string s, CancellationToken t) => null!; }", "SHAF008", "Label");

    [Fact]
    public void EntityWithoutQuery()
        => AssertDiagnostic("[AppEntity(\"thing\")] public record Thing(string Id, string Name);", "SHAF009", "Thing");

    [Fact]
    public void DuplicateFunctionIds()
        => AssertDiagnostic($"[AppFunction(\"f\")] public record F(string Name) : IAppFunction<string>; {Handler} [AppFunction(\"f\")] public record G : IAppFunction; public class GH : IAppFunctionHandler<G> {{ public Task Handle(G r, AppFunctionContext c, CancellationToken t) => Task.CompletedTask; }}", "SHAF010", "F, G");

    [Fact]
    public void FunctionIdCollidesWithGeneratedSearch()
        => AssertDiagnostic("[AppEntity(\"thing\")] public record Thing(string Id, string Name); public class Q : IAppEntityQuery<Thing> { public Task<IReadOnlyList<Thing>> GetByIds(IReadOnlyList<string> ids, CancellationToken t) => null!; public Task<IReadOnlyList<Thing>> Search(string s, CancellationToken t) => null!; } [AppFunction(\"search_thing\")] public record F(string Name) : IAppFunction<string>; " + Handler, "SHAF010", "search_thing");

    [Fact]
    public void ShortcutPhraseMustMentionTheApp()
        => AssertDiagnostic($"[AppFunction(\"f\")] [AppShortcut(\"Do the thing\")] public record F(string Name) : IAppFunction<string>; {Handler}", "SHAF011", "Do the thing");

    [Fact]
    public void TooManyShortcuts()
    {
        var functions = string.Join("\n", Enumerable.Range(0, 11).Select(i =>
            $"[AppFunction(\"f{i}\")] [AppShortcut(\"Run {i} in ${{applicationName}}\")] public record F{i} : IAppFunction; public class H{i} : IAppFunctionHandler<F{i}> {{ public Task Handle(F{i} r, AppFunctionContext c, CancellationToken t) => Task.CompletedTask; }}"));
        AssertDiagnostic(functions, "SHAF012", "11");
    }

    [Fact]
    public void HandlerWithoutPublicConstructor()
        => AssertDiagnostic("[AppFunction(\"f\")] public record F(string Name) : IAppFunction<string>; public class H : IAppFunctionHandler<F, string> { H() { } public Task<string> Handle(F r, AppFunctionContext c, CancellationToken t) => Task.FromResult(\"\"); }", "SHAF013");

    [Fact]
    public void NoAppFunctionsReference_GeneratesNothing()
    {
        var compilation = CSharpCompilation.Create("Plain", [CSharpSyntaxTree.ParseText("public class C { }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var result = CSharpGeneratorDriver.Create(new AppFunctionsGenerator()).RunGenerators(compilation).GetRunResult();
        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("Name", "name")]
    [InlineData("URLPath", "urlPath")]
    [InlineData("ID", "id")]
    [InlineData("iPhone", "iPhone")]
    public void CamelCase_MatchesSystemTextJson(string input, string expected)
    {
        Assert.Equal(expected, Analyzer.CamelCase(input));
        Assert.Equal(System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(input), Analyzer.CamelCase(input));
    }
}
