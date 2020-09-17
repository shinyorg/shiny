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
        static IServiceProvider Create(Action<TestSettings> addSettings = null, Action<IServiceCollection> addServices = null)
        {
            var serializer = new ShinySerializer();
            var settings = new TestSettings(serializer);
            addSettings?.Invoke(settings);

            var services = new ShinyServiceCollection();
            services.AddSingleton<IFullService, FullService>();
            services.AddSingleton<ISerializer>(serializer);
            services.AddSingleton<ISettings>(settings);
            addServices?.Invoke(services);

            var sp = services.BuildShinyServiceProvider(true);
            return sp;
        }


        [Fact]
        public void PostBuildRunsOnlyOnItsContainer()
        {
            var reg1 = 0;
            var reg2 = 0;
            var post1 = 0;
            var post2 = 0;

            var module1 = new TestModule(
                () => reg1++,
                () => post1++
            );
            var module2 = new TestModule(
                () => reg2++,
                () => post2++
            );
            Create(null, s => s.RegisterModule(module1));
            Create(null, s => s.RegisterModule(module2));
            reg1.Should().Be(1);
            reg2.Should().Be(1);

            post1.Should().Be(1);
            post2.Should().Be(1);
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
            var key = $"{typeof(FullService).FullName}.Count";
            var count = 6;
            ISettings settings = null;
            var sp = Create(s =>
            {
                s.Set(key, count);
                settings = s;
            });
            var service = sp.Resolve<IFullService>(true);
            service.Count.Should().Be(7);
            settings.Get<int>(key).Should().Be(7);
        }


        [Fact]
        public void StartupAndBindableInstantiateOnce()
        {
            var key = $"{typeof(FullService).FullName}.Count";
            var count = 99;
            ISettings settings = null;
            var sp = Create(s =>
            {
                s.Set(key, count);
                settings = s;
            });
            var service1 = sp.Resolve<IFullService>(true);
            service1.Count.Should().Be(100);

            var service2 = sp.Resolve<IFullService>(true);
            service2.Count.Should().Be(100);
        }
    }
}

