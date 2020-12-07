using System;
using FluentAssertions;
using Shiny.Generators.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    // assembly attributes aren't being picked up in the test harness
    public class StaticClassSourceGeneratorTests : AbstractSourceGeneratorTests<StaticClassSourceGenerator>
    {
        public StaticClassSourceGeneratorTests(ITestOutputHelper output) : base(output, "Shiny", "Shiny.Core") { }


        [Fact]
        public void CoreClasses()
        {
            this.Generator.AddSource("[assembly:Shiny.GenerateStaticClassesAttribute(\"Tests\")]");
            this.RunGenerator();

            this.AssertTypes(
                "Tests.ShinyJobs",
                "Tests.ShinyConnectivity",
                "Tests.ShinyPower"
            );
        }


        [Fact]
        public void BleClient()
        {
            this.Generator.AddReferences("Shiny.BluetoothLE", "Shiny.BluetoothLE.Abstractions");
            this.Generator.AddSource("[assembly:Shiny.GenerateStaticClassesAttribute(\"Tests\")]");
            this.RunGenerator();

            this.AssertTypes("Tests.ShinyBle");
        }


        protected override StaticClassSourceGenerator Create() => new StaticClassSourceGenerator("Tests");

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
