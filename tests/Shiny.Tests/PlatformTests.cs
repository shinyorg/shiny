using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Tests;


[Trait("Category", "Platform")]
public class PlatformTests : AbstractShinyTests
{
    public PlatformTests(ITestOutputHelper output) : base(output) {}


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
        var value = "";
        await platform.InvokeOnMainThreadAsync(() =>
        {
            value = "HI";
        });
        value.Should().Be("HI");
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
}
