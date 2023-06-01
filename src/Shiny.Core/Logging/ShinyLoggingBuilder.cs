using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shiny.Logging;


internal class ShinyLoggingBuilder : ILoggingBuilder
{
    public ShinyLoggingBuilder(IServiceCollection services)
    {
        this.Services = services;
        this.Services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();
        this.Services.AddLogging();
    }

    public IServiceCollection Services { get; }
}