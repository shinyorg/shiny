using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Tests;


[Trait("Category", "Platform")]
public class PlatformTests : AbstractShinyTests
{
    public PlatformTests(ITestOutputHelper output) : base(output) {}

    //[Fact(DisplayName = "Platform - App Build")]
    //public void AppBuild()
    //    => Assert.Equal("1", this.GetService<IPlatform>().AppBuild);


    //[Fact(DisplayName = "Platform - AppID")]
    //public void AppIdentifier()
    //    => Assert.Equal("com.shiny.devicetests", this.GetService<IPlatform>().AppIdentifier);


    //[Fact(DisplayName = "Platform - App Version")]
    //public void AppVersion()
    //    => Assert.Equal("1", this.GetService<IPlatform>().AppVersion);


    [Fact(DisplayName = "Platform - InvokeOnMainThread Async /w Result")]
    public async Task InvokeOnMainThreadAsyncWithResult()
    {
        var platform = this.GetService<IPlatform>();
        var value = Guid.NewGuid();
        var result = await platform.InvokeOnMainThreadAsync(async () => value);
        result.Should().Be(value);
    }


    [Fact(DisplayName = "Platform - InvokeOnMainThread Async")]
    public async Task InvokeOnMainThreadAsync()
    {
        var platform = this.GetService<IPlatform>();
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
                await this.GetService<IPlatform>().InvokeOnMainThreadAsync(() => throw new ArgumentException("Test"));

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
    //    => Assert.Equal("", this.GetService<IPlatform>().MachineName);

    //[Fact]
    //public void Manufacturer()
    //    => Assert.Equal("1", this.GetService<IPlatform>().Manufacturer);

    //[Fact]
    //public void Model()
    //    => Assert.Equal("1", this.GetService<IPlatform>().Model);

    //[Fact]
    //public void OperatingSystem()
    //    => Assert.Equal("1", this.GetService<IPlatform>().OperatingSystem);
}
