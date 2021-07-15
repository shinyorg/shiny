using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging
{
    public class ShinyLoggingBuilder : ILoggingBuilder
    {
        public ShinyLoggingBuilder(IServiceCollection services)
            => this.Services = services;

        public IServiceCollection Services { get; }
    }
}
