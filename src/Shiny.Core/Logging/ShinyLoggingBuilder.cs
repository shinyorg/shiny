using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Logging;


internal class ShinyLoggingBuilder : ILoggingBuilder
{
    public ShinyLoggingBuilder(IServiceCollection services)
    {
        this.Services = services;
        this.Services.AddLogging();
        //this.Services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();
        this.Services.AddLogging();
    }

    public IServiceCollection Services { get; }
}