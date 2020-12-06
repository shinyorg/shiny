using System;
using FluentAssertions;
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
            this.Generator.AddSource("[assembly:Shiny.GenerateStaticClassesAttribute(\"CoreClasses\")]");
            this.RunGenerator();

            this.AssertTypes(
                "CoreClasses.ShinyJobs",
                "CoreClasses.ShinyConnectivity",
                "CoreClasses.ShinyPower",
                "CoreClasses.ShinyFileSystem"
            );
        }


        [Fact]
        public void BleClient()
        {
            this.Generator.AddReferences("Shiny.BluetoothLE", "Shiny.BluetoothLE.Abstractions");
            this.Generator.AddSource("[assembly:Shiny.GenerateStaticClassesAttribute(\"BleTests\")]");
            this.RunGenerator();

            this.AssertTypes("BleTests.ShinyBleManager");
        }


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
