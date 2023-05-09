using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shiny;


internal class ShinyLoggingBuilder : ILoggingBuilder
{
    public ShinyLoggingBuilder(IServiceCollection services)
    {
        this.Services = services;
        this.Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        this.Services.AddLogging();
    }

    public IServiceCollection Services { get; }
}