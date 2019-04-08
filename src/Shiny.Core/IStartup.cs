using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public interface IStartup
    {
        /// <summary>
        /// Configure the service collection
        /// </summary>
        /// <param name="services"></param>
        void ConfigureServices(IServiceCollection services);


        /// <summary>
        /// Customize how the container is built from the ServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        IServiceProvider CreateServiceProvider(IServiceCollection services);


        /// <summary>
        /// After the container is built, but before the application is started
        /// You should not spend more than a second at most here
        /// </summary>
        /// <param name="provider"></param>
        void ConfigureApp(IServiceProvider provider);
    }
}
