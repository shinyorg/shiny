using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Generators.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class StaticClassGeneratorTests
    {
        readonly ITestOutputHelper output;
        public StaticClassGeneratorTests(ITestOutputHelper output)
            => this.output = output;


        [Fact]
        public void GenerateCoreClasses()
        {
            var assembly = new AssemblyGenerator();

            var driver = CSharpGeneratorDriver.Create(new StaticClassGenerator());
            driver.RunGeneratorsAndUpdateCompilation(
                assembly.Create("Test.dll"),
                out var outputCompilation,
                out var diags
            );

            Assert.False(
                diags.Any(x => x.Severity == DiagnosticSeverity.Error),
                "Failed: " + diags.FirstOrDefault()?.GetMessage()
            );
            var output = outputCompilation
                .SyntaxTrees
                .LastOrDefault()?
                .ToString();

            this.output.WriteLine(output ?? "NO OUTPUT");
        }
    }
}
