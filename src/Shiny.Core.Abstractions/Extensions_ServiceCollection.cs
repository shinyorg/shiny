using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class Extensions_ServiceCollection
    {
        readonly static IDictionary<int, List<IShinyModule>> modules = new Dictionary<int, List<IShinyModule>>();
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
        /// Register a module (like a category) of services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="module"></param>
        public static void RegisterModule(this IServiceCollection services, IShinyModule module)
        {
            var hash = services.GetHashCode();
            if (!modules.ContainsKey(hash))
                modules.Add(hash, new List<IShinyModule>());

            // modules should run per registration - since the module often registers the delegate and there can be multiples
            // module events should run once per type (not per registration)
            var exists = modules[hash].Any(x => x.GetType() == module.GetType());
            modules[hash].Add(module);

            if (!exists)
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


        public static IServiceProvider BuildShinyServiceProvider(this IServiceCollection services, bool validateScopes, Func<IServiceCollection, IServiceProvider>? containerBuild = null)
        {
            var hash = services.GetHashCode();

            if (modules.ContainsKey(hash))
                foreach (var module in modules[hash])
                    module.Register(services);

            var provider = containerBuild?.Invoke(services) ?? services.BuildServiceProvider(validateScopes);
            if (postBuild.ContainsKey(hash))
                foreach (var action in postBuild[hash])
                    action(provider);

            return provider;
        }
    }
}
