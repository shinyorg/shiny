using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Infrastructure.DependencyInjection;
using Shiny.Settings;
using Shiny.Testing.Settings;
using Xunit;


namespace Shiny.Tests.Infrastructure
{
    public class DependencyInjectionTests
    {
        static IServiceProvider Create(Action<TestSettings> addSettings = null)
        {
            var serializer = new JsonNetSerializer();
            var settings = new TestSettings(serializer);
            addSettings?.Invoke(settings);

            var services = new ShinyServiceCollection();
            services.AddSingleton<IFullService, FullService>();
            services.AddSingleton<ISerializer>(serializer);
            services.AddSingleton<ISettings>(settings);
            var sp = services.BuildServiceProvider();
            services.RunPostBuildActions(sp);
            return sp;
        }


        [Fact]
        public void ServiceStartupTask() => Create()
            .GetService<IFullService>()
            .Count
            .Should()
            .Be(1);


        [Fact]
        public void ServiceRestoresState()
        {
            var value = new Random().Next(1, Int32.MaxValue - 1);
            var sp = Create(s =>
                s.Set(typeof(FullService).FullName + ".Count", value)
            );

            sp
                .GetService<IFullService>()
                .Count
                .Should()
                .Be(value + 1);
        }
    }
}
