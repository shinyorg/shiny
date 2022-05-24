using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny;


internal class ShinyInitializationService : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var host = new Host(services, loggerFactory);
        host.Run();
    }
}
