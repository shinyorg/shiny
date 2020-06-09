using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.AppState;
using Shiny.Infrastructure;
using Shiny.Infrastructure.DependencyInjection;


namespace Shiny
{
    public abstract class ShinyHost
    {
        internal static List<Action<IServiceProvider>> PostBuildActions { get; } = new List<Action<IServiceProvider>>();


        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
        public static void AddPostBuildAction(Action<IServiceProvider> action) => PostBuildActions.Add(action);


        /// <summary>
        /// Resolve a specified service from the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>() => Container.Resolve<T>();


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Lazy<T> LazyResolve<T>() => new Lazy<T>(() => Container.Resolve<T>());

        /// <summary>
        /// Resolve a list of registered services from the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> ResolveAll<T>() => Container.GetServices<T>();


        /// <summary>
        /// The configured service collection post container set
        /// </summary>
        public static IServiceCollection? Services { get; private set; }


        /// <summary>
        /// Feeds another service container
        /// </summary>
        /// <param name="feed"></param>
        public static void Populate(Action<Type, Func<object>, ServiceLifetime> feed)
        {
            if (Services == null)
                throw new ArgumentException("Services have not yet been initialized");

            foreach (var service in Services)
            {
                feed(
                    service.ServiceType,
                    () => Container.GetService(service.ServiceType),
                    service.Lifetime
                );
            }
        }


        static IServiceProvider? container;
        public static IServiceProvider Container
        {
            get
            {
                if (container == null)
                    throw new ArgumentException("Container has not been setup - have you initialized the Platform provider using ShinyHost.Init?");

                return container;
            }
        }


        /// <summary>
        /// Setting this before calling build will force the internal service builder to validate scopes of DI registrations (THIS IS SLOW - USE IT FOR DEBUGGING)
        /// </summary>
        public static bool ValidateScopes { get; set; }

        protected static void InitPlatform(IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
        {
            var services = new ShinyServiceCollection();

            startup?.ConfigureServices(services);
            platformBuild?.Invoke(services);

            services.TryAddSingleton<IMessageBus, MessageBus>();
            services.TryAddSingleton<IRepository, FileSystemRepositoryImpl>();
            services.TryAddSingleton<ISerializer, ShinySerializer>();

            Services = services;

            container = startup?.CreateServiceProvider(services) ?? services.BuildServiceProvider(ValidateScopes);
            services.RunPostBuildActions(container);
            startup?.ConfigureApp(container);
        }

        public static void OnForeground()
            => Container.GetService<AppStateManager>()?.OnForeground();

        public static void OnBackground()
            => Container.GetService<AppStateManager>()?.OnBackground();
    }
}

