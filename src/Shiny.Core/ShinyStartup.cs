using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Shiny
{
    public interface IShinyStartup
    {
        void ConfigureLogging(ILoggingBuilder builder, IPlatform platform);
        void ConfigureServices(IServiceCollection services, IPlatform platform);
        IServiceProvider? CreateServiceProvider(IServiceCollection services);
    }


    public abstract class ShinyStartup : IShinyStartup
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="platform"></param>
        public virtual void ConfigureLogging(ILoggingBuilder builder, IPlatform platform) { }

        /// <summary>
        /// Configure the service collection
        /// </summary>
        /// <param name="services"></param>
        public abstract void ConfigureServices(IServiceCollection services, IPlatform platform);


        /// <summary>
        /// Customize how the container is built from the ServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public virtual IServiceProvider? CreateServiceProvider(IServiceCollection services) => null;
    }
}
