using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    //public class LazyService<T> : Lazy<T>
    //{
    //    public LazyService(IServiceProvider services) : base(() => services.GetRequiredService<T>(), false) { }
    //}


    public static class ServiceCollectionExtensions
    {
        readonly static IDictionary<IServiceCollection, List<IShinyModule>> modules = new Dictionary<IServiceCollection, List<IShinyModule>>();
        readonly static IDictionary<IServiceCollection, List<Action<IServiceProvider>>> postBuild = new Dictionary<IServiceCollection, List<Action>>();



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
            var exists = modules.Any(x => x.GetType() == module.GetType());
            if (!exists)
            {
                modules[services].Add(module);
            }
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


        public static IServiceProvider BuildShinyContainer(this IServiceCollection services, bool validateScopes)
        {
            var mods = modules[services]?.AsEnumerable() ?? Enumerable.Empty<IShinyModule>();
            foreach (var mod in mods)
                mod.Register(services);

            var provider = services.CreateServiceProvider(validateScopes);
            return provider;
        }
    }
}
