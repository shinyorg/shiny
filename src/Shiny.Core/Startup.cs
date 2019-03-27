using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public abstract class Startup
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
        public virtual IServiceProvider CreateServiceProvider(IServiceCollection services)
            => services.BuildServiceProvider();


        /// <summary>
        /// After the container is built, but before the application is started
        /// You should not spend more than a second at most here
        /// </summary>
        /// <param name="provider"></param>
        public virtual void ConfigureApp(IServiceProvider provider)
        {
            var tasks = provider.GetServices<IStartupTask>();
            foreach (var task in tasks)
                task.Start();
        }
    }
}
