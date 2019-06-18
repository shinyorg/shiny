using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Logging;

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


        /// <summary>
        /// Feeds another service container
        /// </summary>
        /// <param name="feed"></param>
        public static void Populate(Action<Type, Func<object>, ServiceLifetime> feed)
        {
            foreach (var service in Services)
            {
                feed(
                    service.ServiceType,
                    () => Container.GetService(service.ServiceType),
                    service.Lifetime
                );
            }
        }


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


        protected static void InitPlatform(IStartup startup = null, Action<IServiceCollection> platformBuild = null)
        {
            var services = new ServiceCollection();

            // add standard infrastructure
            services.AddSingleton<IMessageBus, MessageBus>();

            startup?.ConfigureServices(services);
            platformBuild?.Invoke(services);
            Services = services;

            container = startup?.CreateServiceProvider(services) ?? services.BuildServiceProvider();
            startup?.ConfigureApp(container);

            container.RunPostBuildActions();
            var tasks = container.GetServices<IStartupTask>();
            foreach (var task in tasks)
                task.Start();
        }
    }
}

