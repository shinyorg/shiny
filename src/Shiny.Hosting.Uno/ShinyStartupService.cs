#if PLATFORM
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Uno.Extensions.Hosting;

namespace Shiny.Hosting;


public class ShinyStartupService : IStartupService
{
    readonly IServiceProvider services;
    public ShinyStartupService(IServiceProvider services)
        => this.services = services;


    public Task StartupComplete()
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var host = new Host(services, loggerFactory);
        host.Run();

        return Task.CompletedTask;
    }
}
#endif