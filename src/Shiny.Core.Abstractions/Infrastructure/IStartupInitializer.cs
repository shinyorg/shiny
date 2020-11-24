using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    public interface IShinyStartupInitializer
    {
        void Initialize(IShinyStartup startup, IServiceCollection services);
    }
}
