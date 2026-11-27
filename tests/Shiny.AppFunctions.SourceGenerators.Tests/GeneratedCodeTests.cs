using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shiny.AppFunctions.SourceGenerators.Tests;

/// <summary>Runs the generator and compiles its output, so code the generator writes but the compiler rejects fails here.</summary>
public class GeneratedCodeTests
{
    static (IReadOnlyList<Diagnostic> Errors, string Generated) Compile(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(x => MetadataReference.CreateFromFile(x))
            .Append(MetadataReference.CreateFromFile(typeof(AppFunctionAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions).Assembly.Location))
            .GroupBy(x => x.FilePath)
            .Select(x => x.First());

        var compilation = CSharpCompilation.Create(
            "Test",
            [CSharpSyntaxTree.ParseText("global using System; global using System.Threading; global using System.Threading.Tasks; global using System.Collections.Generic; global using Shiny.AppFunctions;\n" + source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );

        CSharpGeneratorDriver.Create(new AppFunctionsGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        Assert.Empty(generatorDiagnostics);
        var generated = output.SyntaxTrees.Skip(1).Single().ToString();
        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        return (errors, generated);
    }

    const string ThingQuery = "public class Q : IAppEntityQuery<Thing> { public Task<IReadOnlyList<Thing>> GetByIds(IReadOnlyList<string> ids, CancellationToken t) => null!; public Task<IReadOnlyList<Thing>> Search(string s, CancellationToken t) => null!; }";

    [Fact]
    public void RequiredMember_WithOptionalDefaultedMember_Compiles()
    {
        var (errors, generated) = Compile(
            "[AppFunction(\"f\")] public class F : IAppFunction { public required string Name { get; init; } public int Count { get; set; } = 5; } " +
            "public class H : IAppFunctionHandler<F> { public Task Handle(F r, AppFunctionContext c, CancellationToken t) => Task.CompletedTask; }"
        );
        Assert.Empty(errors);
        Assert.Contains("var defaults = new global::F() { Name = p0 };", generated);
    }

    [Fact]
    public void ValueTypeDisplayProperty_Compiles()
    {
        var (errors, generated) = Compile("[AppEntity(\"thing\", DisplayProperty = \"Number\")] public record Thing(string Id, int Number); " + ThingQuery);
        Assert.Empty(errors);
        Assert.Contains("e.Number.ToString()", generated);
    }

    [Fact]
    public void ReferenceDisplayProperty_StillNullChecks()
    {
        var (errors, generated) = Compile("[AppEntity(\"thing\")] public record Thing(string Id, string? Name); " + ThingQuery);
        Assert.Empty(errors);
        Assert.Contains("e.Name?.ToString() ?? e.Id", generated);
    }

    [Fact]
    public void SameSimpleNames_GetDistinctSwiftNames()
    {
        var (errors, generated) = Compile(
            "namespace A { public enum Status { Open, Closed } [AppFunction(\"a_create\")] public record CreateItem(Status Status) : IAppFunction; public class H : IAppFunctionHandler<CreateItem> { public Task Handle(CreateItem r, AppFunctionContext c, CancellationToken t) => Task.CompletedTask; } }\n" +
            "namespace B { public enum Status { Low, High } [AppFunction(\"b_create\")] public record CreateItem(Status Status) : IAppFunction; public class H : IAppFunctionHandler<CreateItem> { public Task Handle(CreateItem r, AppFunctionContext c, CancellationToken t) => Task.CompletedTask; } }\n" +
            "public enum Priority { Low, High } [AppFunction(\"single\")] public record Single(Priority Priority) : IAppFunction; public class SH : IAppFunctionHandler<Single> { public Task Handle(Single r, AppFunctionContext c, CancellationToken t) => Task.CompletedTask; }"
        );
        Assert.Empty(errors);

        Assert.Contains("enum A_StatusAppEnum:", generated);
        Assert.Contains("enum B_StatusAppEnum:", generated);
        Assert.Contains("struct A_CreateItemIntent:", generated);
        Assert.Contains("struct B_CreateItemIntent:", generated);
        Assert.Contains("var status: A_StatusAppEnum", generated);
        Assert.Contains("var status: B_StatusAppEnum", generated);

        // unique names are left alone
        Assert.Contains("enum PriorityAppEnum:", generated);
        Assert.Contains("struct SingleIntent:", generated);
        Assert.DoesNotContain("enum StatusAppEnum:", generated);
    }
}
