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


        [Fact(DisplayName = "Platform - InvokeOnMainThread Async /w Error")]
        public async Task InvokeOnMainThreadAsyncError()
        {
            try
            {
                await Task.Run(async () =>
                {
                    await ShinyHost
                        .Resolve<IPlatform>()
                        .InvokeOnMainThreadAsync(() => throw new ArgumentException("Test"));

                    throw new Exception("If you got here, the test failed");
                });
            }
            catch (ArgumentException ex) when (ex.Message.Equals("Test"))
            {
                // swallow, good here
            }
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
