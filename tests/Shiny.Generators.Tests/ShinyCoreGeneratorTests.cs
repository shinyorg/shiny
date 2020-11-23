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


        // TODO: build xam ios libs, add appdelegate
        // test for finding custom shiny startup
        // test for auto-gen shiny startup
        // test for methods
        // test with/out XF
        // test for auto-init of 3rd party
        // ensure appdelegate methods are generated only when necessary!

        // TODO: build android
        // test for finding custom shiny startup
        // test for auto-gen shiny startup
        // test with/out XF
        // test for auto-init of 3rd party
        // test for activity
            // test for permissions
            // test for init
            // test for partial

        // TODO: auto startup
        // add shiny libs one by one to ensure output (push has some special considerations)
        // test for xam forms auto init

        [Fact]
        public void Test()
        {
            var assembly = new AssemblyGenerator();

            assembly.AddSource(@"
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


        void DoGenerate(AssemblyGenerator assembly)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.generatordriver?view=roslyn-dotnet
            var driver = CSharpGeneratorDriver.Create(new ShinyCoreGenerator());
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