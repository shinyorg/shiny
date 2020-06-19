using System;
using System.Linq;

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
            var serializer = new ShinySerializer();
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


        [Fact]
        public void ResolveManyWithManyRegistered()
        {
            var services = new ServiceCollection();
            services.AddSingleton<InterfaceType, ImplType1>();
            services.AddSingleton<InterfaceType, ImplType2>();
            services.AddSingleton<InterfaceType, ImplType3>();

            var container = services.BuildServiceProvider();
            var impls = container.ResolveAll<InterfaceType>();
            impls.Should().NotBeNull();
            impls.Count().Should().Be(3);
        }


        [Fact]
        public void ResolveManyWithOnlyOneRegistered()
        {
            var services = new ServiceCollection();
            services.AddSingleton<InterfaceType, ImplType1>();

            var container = services.BuildServiceProvider();
            var impls = container.ResolveAll<InterfaceType>();
            impls.Should().NotBeNull();
            impls.Count().Should().Be(1);
        }


        [Fact]
        public void ResolveManyWithNoneRegistered()
        {
            var services = new ServiceCollection();
            var container = services.BuildServiceProvider();
            var impls = container.ResolveAll<InterfaceType>();
            impls.Should().NotBeNull();
            impls.Count().Should().Be(0);
        }
    }

    public interface InterfaceType {}
    public class ImplType1 : InterfaceType { }
    public class ImplType2 : InterfaceType { }
    public class ImplType3 : InterfaceType { }
}

