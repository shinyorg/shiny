using System;
using Shiny.Generators.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class StaticClassSourceGeneratorTests : AbstractSourceGeneratorTests<StaticClassSourceGenerator>
    {
        public StaticClassSourceGeneratorTests(ITestOutputHelper output) : base(output, "Shiny", "Shiny.Core") { }


        [Fact]
        public void CoreClasses()
        {
            this.Generator.AddSource("[assembly:Shiny.GenerateStaticClasses(\"CoreClasses\")]");
            this.RunGenerator();

            //compile.AssertTypesExist(
            //    "CoreClasses.ShinyJobs",
            //    "CoreClasses.ShinyConnectivity",
            //    "CoreClasses.ShinyPower",
            //    "CoreClasses.ShinyFileSystem"
            //);
        }
    }
}
