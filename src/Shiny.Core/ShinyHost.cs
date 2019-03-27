using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public abstract partial class ShinyHost
    {
        /// <summary>
        /// Resolve a specified service from the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>() => Container.GetService<T>();


        /// <summary>
        /// Resolve a list of registered services from the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> ResolveAll<T>() => Container.GetServices<T>();


        /// <summary>
        /// The configured service collection post container set
        /// </summary>
        public static IServiceCollection Services { get; private set; }


        static IServiceProvider container;
        public static IServiceProvider Container
        {
            get
            {
                if (container == null)
                    throw new ArgumentException("Container has not been setup - have you initialized the Platform provider?");

                return container;
            }
        }


        protected static void InitPlatform(Startup startup = null, Action<IServiceCollection> platformBuild = null)
        {
            var services = new ServiceCollection();

            startup?.ConfigureServices(services);
            platformBuild?.Invoke(services);
            Services = services;

            container = startup?.CreateServiceProvider(services) ?? services.BuildServiceProvider();
            startup?.ConfigureApp(container);
        }
    }
}

