using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Infrastructure.DependencyInjection;
using Shiny.Settings;

namespace Shiny
{
    public abstract class ShinyHost
    {
        static readonly List<Action<IServiceProvider>> postBuildActions = new List<Action<IServiceProvider>>();

        static void RunPostBuildActions(IServiceProvider container)
        {
            foreach (var action in postBuildActions)
                action(container);

            postBuildActions.Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
        public static void AddPostBuildAction(Action<IServiceProvider> action) => postBuildActions.Add(action);

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


        /// <summary>
        /// Setting this before calling build will force the internal service builder to validate scopes of DI registrations (THIS IS SLOW - USE IT FOR DEBUGGING)
        /// </summary>
        public static bool ValidateScopes { get; set; }

        protected static void InitPlatform(IShinyStartup startup = null, Action<IServiceCollection> platformBuild = null)
        {
            var services = new ShinyServiceCollection();

            // add standard infrastructure
            services.AddSingleton<IMessageBus, MessageBus>();
            services.AddSingleton<IRepository, FileSystemRepositoryImpl>();
            services.AddSingleton<ISerializer, JsonNetSerializer>();

            startup?.ConfigureServices(services);
            platformBuild?.Invoke(services);
            Services = services;

            container = startup?.CreateServiceProvider(services) ?? services.BuildServiceProvider(ValidateScopes);
            startup?.ConfigureApp(container);
            RunPostBuildActions(container);
        }
    }
}

