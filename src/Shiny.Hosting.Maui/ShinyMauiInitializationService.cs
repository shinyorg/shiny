using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Shiny.Hosting;

namespace Shiny;


public class ShinyMauiInitializationService : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var host = new Host(services, loggerFactory);
        host.Run();
    }
}
