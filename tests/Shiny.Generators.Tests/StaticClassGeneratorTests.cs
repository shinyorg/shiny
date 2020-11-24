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
        public void GenerateStaticClasses()
        {
            var assembly = new AssemblyGenerator();
            assembly.AddSource("[assembly:Shiny.GenerateStaticClassesAttribute]");

            var driver = CSharpGeneratorDriver.Create(new StaticClassGenerator());
            var inputCompilation = assembly.Create("Test.dll");

            driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompilation,
                out var diags
            );

            Assert.False(
                diags.Any(x => x.Severity == DiagnosticSeverity.Error),
                "Failed: " + diags.FirstOrDefault()?.GetMessage()
            );

            foreach (var syntaxTree in outputCompilation.SyntaxTrees)
                this.output.WriteLine(syntaxTree.ToString());
        }
    }
}
