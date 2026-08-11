using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators.Tests;


/// <summary>
/// What one generator run produced.
/// </summary>
public sealed record GeneratorRun(
    IReadOnlyDictionary<string, string> Sources,
    IReadOnlyList<Diagnostic> GeneratorDiagnostics,
    IReadOnlyList<Diagnostic> CompilationDiagnostics
)
{
    /// <summary>Every generated file concatenated - handy for coarse "does it contain" assertions.</summary>
    public string AllSource => String.Join("\n", this.Sources.Values);

    public string Source(string endsWith)
        => this.Sources.First(kv => kv.Key.EndsWith(endsWith, StringComparison.Ordinal)).Value;

    public IReadOnlyList<string> Ids(DiagnosticSeverity severity = DiagnosticSeverity.Error)
        => this.GeneratorDiagnostics
            .Concat(this.CompilationDiagnostics)
            .Where(d => d.Severity >= severity)
            .Select(d => d.Id)
            .Distinct()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
}


/// <summary>
/// Runs <see cref="BleHostingGenerator"/> over a source snippet in memory.
/// </summary>
public static class GeneratorHarness
{
    static readonly ImmutableArray<MetadataReference> References = BuildReferences();


    /// <summary>
    /// Compiles <paramref name="source"/> with the generator attached.
    /// </summary>
    /// <param name="source">The user code under test.</param>
    /// <param name="rootNamespace">Value handed to the generator as build_property.RootNamespace.</param>
    /// <returns>The generated sources plus every diagnostic raised.</returns>
    public static GeneratorRun Run(string source, string rootNamespace = "TestApp")
    {
        var compilation = CSharpCompilation.Create(
            "TestApp",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );

        var driver = CSharpGeneratorDriver
            .Create(new BleHostingGenerator())
            .WithUpdatedAnalyzerConfigOptions(new OptionsProvider(rootNamespace))
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        var result = driver.GetRunResult();
        var sources = result.Results
            .SelectMany(r => r.GeneratedSources)
            .ToDictionary(s => s.HintName, s => s.SourceText.ToString(), StringComparer.Ordinal);

        return new GeneratorRun(
            sources,
            generatorDiagnostics,
            // the generated code has to actually compile, not just look right
            output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList()
        );
    }


    static ImmutableArray<MetadataReference> BuildReferences()
    {
        var assemblies = new HashSet<Assembly>
        {
            typeof(object).Assembly,
            typeof(BleServiceAttribute).Assembly,              // Abstractions
            typeof(IBleHostingManager).Assembly,               // Shiny.BluetoothLE.Hosting
            typeof(Shiny.BluetoothLE.L2CapChannel).Assembly,   // Shiny.BluetoothLE.Common
            typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly,
            typeof(System.Reactive.Unit).Assembly
        };

        // pull in the whole shared framework so byte[], Task, IAsyncEnumerable etc all resolve
        var trusted = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? String.Empty)
            .Split(System.IO.Path.PathSeparator)
            .Where(path => path.Length > 0);

        var references = trusted
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();

        foreach (var assembly in assemblies.Where(a => !String.IsNullOrEmpty(a.Location)))
        {
            if (!trusted.Contains(assembly.Location))
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        return references.ToImmutableArray();
    }


    sealed class OptionsProvider(string rootNamespace) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(rootNamespace);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => this.GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => this.GlobalOptions;


        sealed class Options(string rootNamespace) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value)
            {
                if (key == "build_property.RootNamespace")
                {
                    value = rootNamespace;
                    return true;
                }
                value = String.Empty;
                return false;
            }
        }
    }
}
