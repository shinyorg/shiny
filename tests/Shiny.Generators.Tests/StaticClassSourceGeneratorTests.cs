using System;
using Shiny.Generators.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class StaticClassSourceGeneratorTests
    {
        readonly ITestOutputHelper output;
        readonly AssemblyGenerator generator;


        public StaticClassSourceGeneratorTests(ITestOutputHelper output)
        {
            this.output = output;
            this.generator = new AssemblyGenerator();
            this.generator.AddReference("Shiny");
            this.generator.AddReference("Shiny.Core");
        }


        [Fact]
        public void CoreClasses()
        {
            this.generator.AddSource("[assembly:Shiny.GenerateStaticClasses(\"CoreClasses\")]");
            var compile = this.generator.DoGenerate(
                nameof(CoreClasses),
                new StaticClassSourceGenerator()
            );
            this.output.WriteSyntaxTrees(compile);

            compile.AssertTypesExist(
                "CoreClasses.ShinyJobs",
                "CoreClasses.ShinyConnectivity",
                "CoreClasses.ShinyPower",
                "CoreClasses.ShinyFileSystem"
            );
        }
    }
}
