using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


public interface IHostBuilder
{
    IServiceCollection Services { get; }
    //ConfigurationManager? Configuration { get; }
    ILifecycleBuilder Lifecycle { get; }
    ILoggingBuilder Logging { get; }

    IHost Build();
}
