using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Logging;


namespace Shiny
{
    //public class LazyService<T> : Lazy<T>
    //{
    //    public LazyService(IServiceProvider services) : base(() => services.GetRequiredService<T>(), false) { }
    //}


    public static class ServiceExtensions
    {
        //public static void AddLazySingleton<TService, TImpl>(this IServiceCollection services)
        //    => services.AddSingleton<Lazy<TService>, LazyService<TImpl>>();


        /// <summary>
        /// Get Service of Type T from the IServiceProvider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <param name="requiredService"></param>
        /// <returns></returns>
        public static T Resolve<T>(this IServiceProvider serviceProvider, bool requiredService = false)
            => requiredService ? serviceProvider.GetRequiredService<T>() : serviceProvider.GetService<T>();


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        public static async Task SafeResolveAndExecute<T>(this IServiceProvider services, Func<T, Task> execute, bool requiredService = true)
        {
            try
            {
                var service = services.Resolve<T>(requiredService);
                if (service != null)
                    await execute.Invoke(service).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }


        public static Task RunDelegates<T>(this IServiceProvider services, Func<T, Task> execute, Action<Exception>? onError = null)
            => services.GetServices<T>().RunDelegates(execute, onError);


        public static async Task RunDelegates<T>(this IEnumerable<T> services, Func<T, Task> execute, Action<Exception>? onError = null)
        {
            if (services == null)
                return;

            var tasks = services
                .Select(async x =>
                {
                    try
                    {
                        await execute(x).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (onError == null)
                            Log.Write(ex);
                        else
                            onError(ex);
                    }
                })
                .ToList();

            await Task.WhenAll(tasks);
        }


        //public static async Task RunResolves<T>(this IServiceProvider services, Func<T, Task> execute)
        //{
        //    var resolves = services.GetServices<T>();
        //    if (resolves == null)
        //        return;

        //    foreach (var resolve in resolves)
        //    {
        //        try
        //        {
        //            // TODO: run in parallel in case delegates have anything long running
        //            await execute(resolve).ConfigureAwait(false);
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Write(ex);
        //        }
        //    }
        //}

        /// <summary>
        /// Registers a post container build step
        /// </summary>
        /// <param name="services"></param>
        /// <param name="action"></param>
        public static void RegisterPostBuildAction(this IServiceCollection services, Action<IServiceProvider> action)
            => ShinyHost.AddPostBuildAction(action);


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
        /// Check if a service type is registered on the service builder
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static bool IsRegistered<TService>(this IServiceCollection services)
            => services.IsRegistered(typeof(TService));


        /// <summary>
        /// Check if a service type is registered on the service builder
        /// </summary>
        /// <param name="services"></param>
        /// <param name="serviceType"></typeparam>
        /// <returns></returns>
        public static bool IsRegistered(this IServiceCollection services, Type serviceType)
            => services.Any(x => x.ServiceType == serviceType);


        /// <summary>
        /// Check if a service is registered in the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static bool IsRegistered<T>(this IServiceProvider services)
            => services.GetService(typeof(T)) != null;


        /// <summary>
        /// Check if a service is register in the container
        /// </summary>
        /// <param name="services"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static bool IsRegistered(this IServiceProvider services, Type serviceType)
            => services.GetService(serviceType) != null;


        /// <summary>
        /// Asserts that a service type is registered on the service builder
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static void AssertNotRegistered<TService>(this IServiceCollection services)
            => services.AssertNotRegistered(typeof(TService));


        /// <summary>
        /// Asserts that a service type is registered on the service builder
        /// </summary>
        /// <param name="services"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static void AssertNotRegistered(this IServiceCollection services, Type serviceType)
        {
            if (services.IsRegistered(serviceType))
                throw new ArgumentException($"Service type '{serviceType.FullName}' is already registered");
        }
    }
}
