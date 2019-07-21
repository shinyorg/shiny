using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure.DependencyInjection;
using Xunit;


namespace Shiny.Tests.Infrastructure
{
    public class DependencyInjectionTests
    {
        [Fact(Skip = "TODO")]
        public void DoesStartup()
        {
            var services = new ShinyServiceCollection();
            services.AddSingleton<IFullService, FullService>();
            var sp = services.BuildServiceProvider();
            sp.GetService<IFullService>().Count.Should().Be(1);
        }


        [Fact(Skip = "TODO")]
        public void DoesBind()
        {
            var settings = new InMemorySetttings();
            //settings.("FullService.Count", 3);
        }
    }
}
