using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    public interface IStartupInitializer
    {
        void Initialize(ShinyStartup startup, IServiceCollection services);
    }
}
