using Microsoft.Extensions.DependencyInjection;
using Shiny.GameCenter;
using Shiny.GameCenter.Infrastructure;
using Xunit;

namespace Shiny.GameCenter.Tests;


public class RegistrationTests : IDisposable
{
    readonly string dir = Path.Combine(Path.GetTempPath(), "shiny-gc-reg-" + Guid.NewGuid().ToString("N"));


    [Fact]
    public void AddGameCenter_ResolvesByType()
    {
        Directory.CreateDirectory(this.dir);
        var services = new ServiceCollection();
        services.AddLogging();
        // registered first so the test never touches the real app data folder
        services.AddSingleton(new GameCenterStateStore(Path.Combine(this.dir, "state.json")));
        services.AddGameCenter();

        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        var manager = sp.GetRequiredService<IGameCenterManager>();
        Assert.Same(manager, sp.GetRequiredService<GameCenterManager>());
        Assert.Contains(sp.GetServices<IShinyStartupTask>(), x => ReferenceEquals(x, manager));
    }


    public void Dispose()
    {
        if (Directory.Exists(this.dir))
            Directory.Delete(this.dir, true);
    }
}
