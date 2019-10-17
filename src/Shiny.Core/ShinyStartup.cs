using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public interface IShinyStartup
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



    class InternalShinyStartup : ShinyStartup
    {
        readonly IShinyModule module;
        public InternalShinyStartup(IShinyModule module)
            => this.module = module;

        public override void ConfigureServices(IServiceCollection services)
            => services.RegisterModule(this.module);
    }


    public abstract class ShinyStartup : IShinyStartup
    {
        public static IShinyStartup FromAssemblyRegistration(params Assembly[] assemblies)
            => new InternalShinyStartup(new AssemblyServiceModule(assemblies));


        public static IShinyStartup AutoRegister()
            => new InternalShinyStartup(new AutoRegisterModule());


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


        /// <summary>
        /// After the container is built, but before the application is started
        /// You should not spend more than a second at most here
        /// </summary>
        /// <param name="provider"></param>
        public virtual void ConfigureApp(IServiceProvider provider)
        {
        }
    }
}
