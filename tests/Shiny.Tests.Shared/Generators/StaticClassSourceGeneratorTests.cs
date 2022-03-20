using System;
using FluentAssertions;
using Shiny.Generators;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Tests.Generators
{
    public class StaticClassSourceGeneratorTests : AbstractSourceGeneratorTests<StaticClassSourceGenerator>
    {
        public StaticClassSourceGeneratorTests(ITestOutputHelper output) : base(output, "Shiny", "Shiny.Core")
        {
            this.Generator.AddReference("Microsoft.Extensions.DependencyInjection");
            this.Generator.AddReference("Microsoft.Extensions.DependencyInjection.Abstractions");
        }


        [Fact]
        public void CoreClasses()
        {
            this.Generator.AddSource("[assembly:Shiny.GenerateStaticClassesAttribute(\"Tests\")]");
            this.RunGenerator(false);

            this.AssertTypes(
                "Tests.ShinyJobs",
                "Tests.ShinyConnectivity",
                "Tests.ShinyPower"
            );
        }


        [Fact]
        public void BleClient()
        {
            this.Generator.AddReferences("Shiny.BluetoothLE");
            this.Generator.AddSource("[assembly:Shiny.GenerateStaticClassesAttribute(\"Tests\")]");
            this.RunGenerator(false);

            this.AssertTypes("Tests.ShinyBle");
        }


        protected override StaticClassSourceGenerator Create() => new StaticClassSourceGenerator();

        void AssertTypes(params string[] types)
        {
            foreach (var type in types)
            {
                this.Compilation
                    .GetTypeByMetadataName(type)
                    .Should()
                    .NotBeNull(type + " static not created");
            }
        }
    }
}
