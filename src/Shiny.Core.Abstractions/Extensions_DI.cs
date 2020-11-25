using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class Extensions_DI
    {
        readonly static IDictionary<int, List<Action<IServiceProvider>>> postBuild = new Dictionary<int, List<Action<IServiceProvider>>>();


        /// <summary>
        /// Registers a post container build step
        /// </summary>
        /// <param name="services"></param>
        /// <param name="action"></param>
        public static void RegisterPostBuildAction(this IServiceCollection services, Action<IServiceProvider> action)
        {
            var hash = services.GetHashCode();
            if (!postBuild.ContainsKey(hash))
                postBuild.Add(hash, new List<Action<IServiceProvider>>());
            postBuild[hash].Add(action);
        }


        /// <summary>
        /// Attempts to resolve or build an instance from a service provider
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static object ResolveOrInstantiate(this IServiceProvider services, Type type)
            => ActivatorUtilities.GetServiceOrCreateInstance(services, type);


        /// <summary>
        /// Attempts to resolve or build an instance from a service provider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static T ResolveOrInstantiate<T>(this IServiceProvider services)
            => (T)ActivatorUtilities.GetServiceOrCreateInstance(services, typeof(T));


        /// <summary>
        /// Register a module (like a category) of services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="module"></param>
        public static void RegisterModule(this IServiceCollection services, IShinyModule module)
        {
            module.Register(services);
            services.RegisterPostBuildAction(module.OnContainerReady);
        }


        /// <summary>
        /// Register a module (like a category) of services
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        public static void RegisterModule<T>(this IServiceCollection services)
            where T : IShinyModule, new() => services.RegisterModule(new T());


        /// <summary>
        /// Get Service of Type T from the IServiceProvider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <param name="requiredService"></param>
        /// <returns></returns>
        public static T Resolve<T>(this IServiceProvider serviceProvider, bool requiredService = false)
            => requiredService ? serviceProvider.GetRequiredService<T>() : serviceProvider.GetService<T>();


        public static IServiceProvider BuildShinyServiceProvider(this IServiceCollection services,
                                                                 bool validateScopes,
                                                                 Func<IServiceCollection, IServiceProvider>? containerBuild = null,
                                                                 Action<IServiceProvider>? assignBeforeEvents = null)
        {
            var provider = containerBuild?.Invoke(services) ?? services.BuildServiceProvider(validateScopes);
            assignBeforeEvents?.Invoke(provider);

            var hash = services.GetHashCode();
            if (postBuild.ContainsKey(hash))
                foreach (var action in postBuild[hash])
                    action(provider);

            return provider;
        }
    }
}
