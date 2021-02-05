using System;
using FluentAssertions;
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
            this.Generator.AddSource("[assembly:Shiny.GenerateStaticClassesAttribute(\"Tests\")]");
            this.RunGenerator();

            this.AssertTypes(
                "Tests.ShinyJobs",
                "Tests.ShinyConnectivity",
                "Tests.ShinyPower",
                "Tests.ShinySettings"
            );
        }


        [Fact]
        public void BleClient()
        {
            this.Generator.AddReferences("Shiny.BluetoothLE");
            this.Generator.AddSource("[assembly:Shiny.GenerateStaticClassesAttribute(\"Tests\")]");
            this.RunGenerator();

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
