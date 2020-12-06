using System;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class StartupGenerationTests : AbstractSourceGeneratorTests<AndroidApplicationSourceGenerator>
    {
        public StartupGenerationTests(ITestOutputHelper output) : base(output, "Mono.Android", "Shiny", "Shiny.Core") {}


        [Fact]
        public void Standard()
        {
            this.Generator.AddSource("[assembly: Shiny.ShinyApplicationAttribute]");
            this.RunGenerator();
        }


        [Fact]
        public void DetectsAndWiresInXfDependencyService()
        {
            this.Generator.AddReference("Xamarin.Forms");
            this.Generator.AddSource("[assembly: Shiny.ShinyApplicationAttribute]");
            this.RunGenerator();
            this.Compilation.AssertHasContent("global::Xamarin.Forms.Internals.DependencyResolver.ResolveUsing(t => provider.GetService(t));");
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

            this.Compilation
                .GetTypeByMetadataName("MyTest.DetectionJob")
                .Should()
                .NotBeNull();
        }


        [Fact]
        public void StartupTaskDetection()
        {
            this.Generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute]
using System;
using System.Threading;
using System.Threading.Tasks;


namespace MyTest
{
    public class MyStartupTask : Shiny.IShinyStartupTask
    {
        public void Start() {}
    }
}");
            this.RunGenerator();
            this.Compilation.AssertHasContent("services.AddSingleton<MyTest.MyStartupTask>();");
        }


        [Fact]
        public void ModuleDetection()
        {
            this.Generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute]
using System;
using Microsoft.Extensions.DependencyInjection;


namespace MyTest
{
    public class MyModule : Shiny.ShinyModule
    {
        public override void Register(IServiceCollection services) {}
    }
}");
            this.RunGenerator();
            this.Compilation.AssertHasContent("services.RegisterModule<MyTest.MyModule>();");
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

            this.Compilation.AssertHasContent("services.UseGps<Test.TestGpsDelegate>();");
        }
    }
}
