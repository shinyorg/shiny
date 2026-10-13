using Microsoft.Extensions.DependencyInjection;
using Shiny.Gamepad;
using Xunit;

namespace Shiny.Gamepad.Tests;


public class RegistrationTests
{
    [Fact]
    public void AddNotSupportedGamepads_ResolvesByType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotSupportedGamepads();

        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        Assert.NotNull(sp.GetRequiredService<IGamepadManager>());
    }
}
