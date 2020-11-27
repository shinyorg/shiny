using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public abstract class ShinyModule
    {
        public abstract void Register(IServiceCollection services);
        public virtual void OnContainerReady(IServiceProvider services) { }
    }
}
