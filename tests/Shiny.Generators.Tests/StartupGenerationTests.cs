using System;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class StartupGenerationTests : AbstractSourceGeneratorTests<AndroidApplicationSourceGenerator>
    {
        public StartupGenerationTests(ITestOutputHelper output) : base(output, "Mono.Android", "Shiny", "Shiny.Core")
        {
        }


        [Fact]
        public void Test()
        {
            this.Generator.AddSource("[assembly: Shiny.ShinyApplicationAttribute]");
            this.RunGenerator();
        }


        [Fact]
        public void JobDetection()
        {
            this.Generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute]
using System;
using System.Threading;
using System.Threading.Tasks;


namespace MyTest
{
    public class DetectionJob : Shiny.Jobs.IJob
    {
        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken) {}
    }
}");
            this.RunGenerator();
            // TODO: find job
        }


        [Fact]
        public void ExistingStartupDetectionSameAssembly()
        {
            this.Generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute]
using System;
using System.Threading;
using System.Threading.Tasks;


namespace MyTest
{
    public class ExistingStartup : Shiny.ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services) {}
    }
}");
            this.RunGenerator();

            this.Compilation.GetTypeByMetadataName("MyTest.AppShinyStartup").Should().BeNull("it shouldn't have been auto-generated");
            this.Compilation.GetTypeByMetadataName("MyTest.ExistingStartup").Should().NotBeNull("it was created");
        }


        public void ExistingStartupDefined()
        {
        }


        public void ModuleDetection()
        {
        }


        public void StartupTaskDetection()
        {
        }


        [Fact]
        public void DelegateDetection()
        {
            this.Generator.AddReference("Shiny.Locations");
            this.Generator.AddReference("Shiny.Locations.Abstractions");
            this.Generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute]
namespace Test
{
    public class TestGpsDelegate : Shiny.Locations.IGpsDelegate
    {
        public Task OnReading(IGpsReading reading) => throw new NotImplementedException();
    }
}");
            this.RunGenerator();

            // TODO: find generator
        }
    }
}
