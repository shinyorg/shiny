using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Infrastructure;
using Xunit;

namespace Shiny.Tests;


public class HostRunTests
{
    [Fact]
    public void Run_TwiceOnSameServiceProvider_StartsTasksOnce()
    {
        var task = new CountingStartupTask();
        var builder = HostBuilder.Create();
        builder.Services.AddSingleton<IShinyStartupTask>(task);
        using var host = builder.Build();

        host.Run();
        new Host(host.Services, host.Services.GetRequiredService<ILoggerFactory>()).Run();

        Assert.Equal(1, task.StartCount);
    }


    [Fact]
    public void Run_AfterDispose_StartsAgain()
    {
        var task = new CountingStartupTask();
        var services = new ServiceCollection();
        services.AddSingleton<IShinyStartupTask>(task);

        var first = HostBuilder.Create(services).Build();
        first.Run();
        first.Dispose();

        using var second = HostBuilder.Create(services).Build();
        second.Run();

        Assert.Equal(2, task.StartCount);
    }


    [Fact]
    public void AddShinyCoreServices_CalledTwice_RegistersOnce()
    {
        var services = new ServiceCollection();
        services.AddShinyCoreServices();
        var count = services.Count;

        services.AddShinyCoreServices();

        Assert.Equal(count, services.Count);
    }


    class CountingStartupTask : IShinyStartupTask
    {
        public int StartCount { get; private set; }
        public void Start() => this.StartCount++;
    }
}
