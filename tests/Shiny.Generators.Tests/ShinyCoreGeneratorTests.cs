using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class ShinyCoreGeneratorTests
    {
        readonly ITestOutputHelper output;
        public ShinyCoreGeneratorTests(ITestOutputHelper output)
            => this.output = output;


        [Fact]
        public void Test()
        {
            this.DoGenerate(@"
namespace Foo
{
    class C
        {
            void M()
            {
            }
        }
    }"
            );
        }


        void DoGenerate(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            //var references = new List<MetadataReference>();
            //Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //foreach (var assembly in assemblies)
            //{
            //    if (!assembly.IsDynamic)
            //    {
            //        references.Add(MetadataReference.CreateFromFile(assembly.Location));
            //    }
            //}

            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.generatordriver?view=roslyn-dotnet
            var driver = CSharpGeneratorDriver.Create(new ShinyCoreGenerator());
            //driver.RunGenerators(CSharpCompilation.Create("Test.dll"));
            driver.RunGeneratorsAndUpdateCompilation(
                CSharpCompilation.Create("Test.dll"),
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

            this.output.WriteLine(output);
        }
    }
}