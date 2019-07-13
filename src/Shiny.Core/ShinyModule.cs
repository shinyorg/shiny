using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public interface IShinyModule
    {
        void Register(IServiceCollection services);
        void OnContainerReady(IServiceProvider services);
    }


    public abstract class ShinyModule : IShinyModule
    {
        public abstract void Register(IServiceCollection services);
        public virtual void OnContainerReady(IServiceProvider services) { }
    }
}
