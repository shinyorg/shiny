using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    //public class LazyService<T> : Lazy<T>
    //{
    //    public LazyService(IServiceProvider services) : base(() => services.GetRequiredService<T>(), false) { }
    //}


    public static class Extensions_ServiceCollection
    {
        readonly static IDictionary<IServiceCollection, List<IShinyModule>> modules = new Dictionary<IServiceCollection, List<IShinyModule>>();
        readonly static IDictionary<IServiceCollection, List<Action<IServiceProvider>>> postBuild = new Dictionary<IServiceCollection, List<Action<IServiceProvider>>>();



        /// <summary>
        /// Registers a post container build step
        /// </summary>
        /// <param name="services"></param>
        /// <param name="action"></param>
        public static void RegisterPostBuildAction(this IServiceCollection services, Action<IServiceProvider> action)
        {
            if (!postBuild.ContainsKey(services))
                postBuild.Add(services, new List<Action<IServiceProvider>>());
            postBuild[services].Add(action);
        }


        //public static void AddLazySingleton<TService, TImpl>(this IServiceCollection services)
        //    => services.AddSingleton<Lazy<TService>, LazyService<TImpl>>();

        /// <summary>
        /// Register a module (like a category) of services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="module"></param>
        public static void RegisterModule(this IServiceCollection services, IShinyModule module)
        {
            if (!modules.ContainsKey(services))
                modules.Add(services, new List<IShinyModule>());

            modules[services].Add(module);
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
            var mods = modules.ContainsKey(services)
                ? modules[services]
                : new List<IShinyModule>(0);

            foreach (var mod in mods)
                mod.Register(services);

            var provider = containerBuild?.Invoke(services) ?? services.BuildServiceProvider(validateScopes);
            foreach (var mod in mods)
                mod.OnContainerReady(provider);

            var actions = postBuild.ContainsKey(services)
                ? postBuild[services]
                : new List<Action<IServiceProvider>>(0);

            foreach (var action in actions)
                action(provider);

            return provider;
        }
    }
}
