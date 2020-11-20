using System;

using Microsoft.CodeAnalysis.CSharp;

namespace Shiny.Generators.Tests
{
    public class ShinyCoreGeneratorTests
    {
        //[Fact]
        public void Test()
        {
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.generatordriver?view=roslyn-dotnet
            var driver = CSharpGeneratorDriver.Create(new ShinyCoreGenerator());
            driver.RunGenerators(CSharpCompilation.Create("Test.dll"));
        }
    }
}
/*
private readonly ITestOutputHelper _output;

	public Tests(ITestOutputHelper output)
	{
            _output = output;
	}

        [Fact]
        public void Test1()
        {
            string source = @"
namespace Foo
{
    class C
    {
        void M()
        {
        }
    }
}";
            string output = GetGeneratedOutput(source);

            Assert.NotNull(output);

            Assert.Equal("class Foo { }", output);
        }

        private string GetGeneratedOutput(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new List<MetadataReference>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (!assembly.IsDynamic)
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            var compilation = CSharpCompilation.Create("foo", new SyntaxTree[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // TODO: Uncomment this line if you want to fail tests when the injected program isn't valid _before_ running generators
            // var compileDiagnostics = compilation.GetDiagnostics();
            // Assert.False(compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), "Failed: " + compileDiagnostics.FirstOrDefault()?.GetMessage());

            ISourceGenerator generator = new Generator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);
            Assert.False(generateDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), "Failed: " + generateDiagnostics.FirstOrDefault()?.GetMessage());

            string output = outputCompilation.SyntaxTrees.Last().ToString();

            _output.WriteLine(output);

            return output;
        }
 */