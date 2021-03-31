using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Infrastructure.DependencyInjection;
using Shiny.Stores;
using Shiny.Testing;
using Shiny.Testing.Stores;
using Xunit;


namespace Shiny.Tests.Core.Infrastructure
{
    public class DependencyInjectionTests
    {
        static IServiceProvider Create(Action<IKeyValueStore> addSettings = null, Action<IServiceCollection> addServices = null)
        {
            var serializer = new ShinySerializer();
            var settings = new TestKeyValueStore();
            addSettings?.Invoke(settings);

            var services = new ShinyServiceCollection();
            services.AddSingleton<IPlatform, TestPlatform>();
            services.RegisterModule(new StoresModule());
            services.AddSingleton<IFullService, FullService>();
            services.AddSingleton<ISerializer>(serializer);
            services.AddSingleton<IKeyValueStore>(settings);
            addServices?.Invoke(services);

            var sp = services.BuildShinyServiceProvider(true);
            return sp;
        }


        static void SetCountKey(IKeyValueStore settings, int value)
        {
            var key = $"{typeof(FullService).FullName}.{nameof(FullService.Count)}";
            settings.Set(key, value);
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
            var s1 = Create(null, s => s.RegisterModule(module1));

            var module2 = new TestModule(
                () => reg2++,
                () => post2++
            );
            var s2 = Create(null, s => s.RegisterModule(module2));
            reg1.Should().Be(1);
            reg2.Should().Be(1);

            post1.Should().Be(1);
            post2.Should().Be(1);
        }


        [Fact]
        public void ServiceRestoresStateAndStartsUp()
        {
            var setValue = new Random().Next(1, 9999);
            var postValue = setValue + 1;

            var services = Create(s => SetCountKey(s, setValue));
            services
                .GetService<IFullService>()
                .Count
                .Should()
                .Be(postValue);
        }
    }
}

