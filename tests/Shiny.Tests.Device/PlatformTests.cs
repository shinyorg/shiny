using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;


namespace Shiny.Tests
{
    [Trait("Category", "Platform")]
    public class PlatformTests
    {
        [Fact(DisplayName = "Platform - App Build")]
        public void AppBuild()
            => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().AppBuild);


        [Fact(DisplayName = "Platform - AppID")]
        public void AppIdentifier()
            => Assert.Equal("com.shiny.devicetests", ShinyHost.Resolve<IPlatform>().AppIdentifier);


        [Fact(DisplayName = "Platform - App Version")]
        public void AppVersion()
            => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().AppVersion);


        [Fact(DisplayName = "Platform - InvokeOnMainThread Sync")]
        public async Task InvokeOnMainThread()
        {
            var platform = ShinyHost.Resolve<IPlatform>();
            var value = 1;
            await Task.Run(() => value = 2).ConfigureAwait(false);

            platform.InvokeOnMainThread(() =>
            {
                value = 99;
            });
            value.Should().Be(99);
        }


        [Fact(DisplayName = "Platform - InvokeOnMainThread Async /w Result")]
        public async Task InvokeOnMainThreadAsyncWithResult()
        {
            var platform = ShinyHost.Resolve<IPlatform>();
            var value = Guid.NewGuid();
            var result = await platform.InvokeOnMainThreadAsync(async () => value);
            result.Should().Be(value);
        }


        [Fact(DisplayName = "Platform - InvokeOnMainThread Async")]
        public async Task InvokeOnMainThreadAsync()
        {
            var platform = ShinyHost.Resolve<IPlatform>();
            var value = Guid.NewGuid();
            await platform.InvokeOnMainThreadAsync(() =>
            {
                Console.WriteLine("HI");
            });
        }

        [Fact(DisplayName = "Platform - InvokeOnMainThread /w Error")]
        public async Task InvokeOnMainThreadError()
        {
            await Task.Run(() =>
            {
                ShinyHost
                    .Resolve<IPlatform>()
                    .InvokeOnMainThread(() => throw new ArgumentException("Test"));
            });
        }

        //// TODO: based on device
        //[Fact]
        //public void MachineName()
        //    => Assert.Equal("", ShinyHost.Resolve<IPlatform>().MachineName);

        //[Fact]
        //public void Manufacturer()
        //    => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().Manufacturer);

        //[Fact]
        //public void Model()
        //    => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().Model);

        //[Fact]
        //public void OperatingSystem()
        //    => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().OperatingSystem);
    }
}
