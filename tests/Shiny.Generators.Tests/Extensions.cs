using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public static class Extensions
    {
        internal static void DoGenerate(this AssemblyGenerator generator, ITestOutputHelper output, string dllName, ISourceGenerator sourceGenerator, params string[] assertTypes)
        {
            var driver = CSharpGeneratorDriver.Create(sourceGenerator);
            var inputCompilation = generator.Create(dllName);

            driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompilation,
                out var diags
            );

            diags
                .Any(x => x.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeFalse(diags.FirstOrDefault()?.GetMessage());

            foreach (var syntaxTree in outputCompilation.SyntaxTrees)
                output.WriteLine(syntaxTree.ToString());

            foreach (var assert in assertTypes)
            {
                var type = outputCompilation.GetTypeByMetadataName(assert);
                type.Should().NotBeNull($"Metadata type '{assert}' was not found");
            }
        }
    }
}
