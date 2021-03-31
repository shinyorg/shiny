using System;
using FluentAssertions;
using Shiny.Generators;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Tests.Generators
{
    public class StartupGenerationTests : AbstractSourceGeneratorTests<AndroidApplicationSourceGenerator>
    {
        public StartupGenerationTests(ITestOutputHelper output) : base(output, "Mono.Android", "Shiny", "Shiny.Core") {}


        ShinyApplicationValues generatorConfig = new ShinyApplicationValues();
        protected override AndroidApplicationSourceGenerator Create() => new AndroidApplicationSourceGenerator
        {
            ShinyConfig = this.generatorConfig
        };


        [Fact]
        public void Standard()
        {
            this.Generator.AddSource("[assembly: Shiny.ShinyApplicationAttribute]");
            this.RunGenerator();
        }


        //[Theory]
        ////[InlineData("Shiny.Push", "services.UsePush<Test.TestPushDelegate>()")]
        //[InlineData("Shiny.Push.AzureNotificationHubs", null)]
        //[InlineData("Shiny.Push.FirebaseMessaging", "services.UseFirebaseMessaging<Test.TestPushDelegate>()")]
        //public void PushRegistration(string lib, string startupRegExpected)
        //{
        //    this.Generator.AddReference(lib);
        //    this.Generator.AddSource("[assembly: Shiny.ShinyApplicationAttribute]");
        //    this.Generator.AddSource(CodeBlocks.PushDelegate);
        //    this.RunGenerator();

        //    if (startupRegExpected == null)
        //        this.Compilation.AssertContent("services.Push", "Push should not be registered", false);
        //    else
        //        this.Compilation.AssertContent(startupRegExpected);
        //}


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
            this.Compilation.AssertContent("services.AddSingleton<MyTest.MyStartupTask>();");
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
            this.Compilation.AssertContent("services.RegisterModule<MyTest.MyModule>();");
        }


        [Theory]
        //[InlineData("Shiny.Push.AzureNotificationHubs", CodeBlocks.PushDelegate, null)]
        [InlineData("Shiny.Push.FirebaseMessaging", CodeBlocks.PushDelegate, "services.UseFirebaseMessaging<Test.TestPushDelegate>();")]
        [InlineData("Shiny.Locations", CodeBlocks.GpsDelegate, "services.UseGps<Test.TestGpsDelegate>();")]
        public void DelegateDetection(string reference, string delegateCodeBlock, string findContent)
        {
            this.Generator.AddReference(reference);
            this.Generator.AddReference("Shiny.Push");
            this.Generator.AddSource("[assembly: Shiny.ShinyApplicationAttribute]");
            this.Generator.AddSource(delegateCodeBlock);
            this.RunGenerator();

            this.Compilation.AssertContent(findContent);
        }


        [Fact]
        public void ServiceRegistration()
        {
            this.Generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute]
namespace Test
{
    public interface ITest1 {}
    public interface ITest2 {}

    [Shiny.ShinyServiceAttribute]
    public class Test1 : ITest1 {}

    [Shiny.ShinyServiceAttribute]
    public class Test2 : ITest1, ITest2 {}

    [Shiny.ShinyServiceAttribute]
    public class Test3 {}
}
");
            this.RunGenerator();
            this.Compilation.AssertContent("services.AddSingleton<Test.ITest1, Test.Test1>()", "should be registered with ITest1 interface");
            this.Compilation.AssertContent("services.AddSingleton<Test.ITest1, Test.Test2>()", "should be registered with ITest1 interface");
            this.Compilation.AssertContent("services.AddSingleton<Test.ITest2, Test.Test2>()", "should be registered with ITest2 interface");
            this.Compilation.AssertContent("services.AddSingleton<Test.Test3>()", "should be registered with no interfaces");
        }
    }
}
