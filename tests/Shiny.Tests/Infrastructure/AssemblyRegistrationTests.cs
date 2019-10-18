using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Infrastructure;
using Shiny.Tests.Infrastructure;
using Xunit;

[assembly: TestServiceModule]
[assembly: ShinyServiceAttribute(typeof(ITestAssemblyService2), typeof(TestAssemblyService2))]


namespace Shiny.Tests.Infrastructure
{
    public class TestServiceModuleAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<ITestAssemblyService, TestAssemblyService>();
        }
    }


    public interface ITestAssemblyService {}
    public class TestAssemblyService : ITestAssemblyService { }

    public interface ITestAssemblyService2 { }
    public class TestAssemblyService2 : ITestAssemblyService2 { }


    public class AssemblyRegistrationTests
    {
        [Fact]
        public void Tests()
        {
            var services = new ServiceCollection();
            services.RegisterModule(new AssemblyServiceModule());
            var sp = services.BuildServiceProvider();

            sp.GetService<ITestAssemblyService>().Should().NotBeNull("TestServiceModuleAttribute didn't run");
            sp.GetService<ITestAssemblyService2>().Should().NotBeNull("ServiceRegister didn't run");
        }
    }
}
