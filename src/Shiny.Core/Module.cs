using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public interface IModule
    {
        void Register(IServiceCollection services);
        void OnContainerReady(IServiceProvider services);
    }


    public abstract class Module : IModule
    {
        public abstract void Register(IServiceCollection services);
        public virtual void OnContainerReady(IServiceProvider services) { }
    }
}
