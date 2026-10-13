using Microsoft.Extensions.DependencyInjection;
using Shiny.AppStore;
using Xunit;

namespace Shiny.AppStore.Tests;


public class RegistrationTests
{
    [Fact]
    public void AddAppStore_ResolvesByType()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPlatform, NetPlatform>();
        services.AddAppStore(linuxAppId: "net.shinylib.Test");

        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        var store = sp.GetRequiredService<IAppStore>();
        Assert.IsType(OperatingSystem.IsLinux() ? typeof(LinuxAppStore) : typeof(NotSupportedAppStore), store);
        Assert.Equal("net.shinylib.Test", sp.GetRequiredService<AppStoreOptions>().LinuxAppId);
    }
}
