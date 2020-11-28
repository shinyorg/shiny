using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public interface IShinyStartup
    {
        void ConfigureServices(IServiceCollection services);
        IServiceProvider CreateServiceProvider(IServiceCollection services);
    }


    public abstract class ShinyStartup : IShinyStartup
    {
        /// <summary>
        /// Configure the service collection
        /// </summary>
        /// <param name="services"></param>
        public abstract void ConfigureServices(IServiceCollection services);


        /// <summary>
        /// Customize how the container is built from the ServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public virtual IServiceProvider CreateServiceProvider(IServiceCollection services) => null;
    }
}
