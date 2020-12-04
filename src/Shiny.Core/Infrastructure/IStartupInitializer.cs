using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    public interface IStartupInitializer
    {
        void Initialize(IShinyStartup startup, IServiceCollection services);
    }
}
