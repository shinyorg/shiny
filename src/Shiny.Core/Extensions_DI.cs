using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Shiny
{
    public static partial class Extensions
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
        public static void RegisterModule(this IServiceCollection services, ShinyModule module)
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
            where T : ShinyModule, new() => services.RegisterModule(new T());


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
                services
                    .Resolve<ILogger<T>>()
                    .LogError(ex, "Error executing delegate");
            }
        }


        public static void RunDelegates<T>(this IServiceProvider serviceProvider, Action<T> execute, Action<Exception>? onError = null)
        {
            var services = serviceProvider.GetServices<T>();
            if (services == null)
                return;

            var logger = serviceProvider.Resolve<ILogger<T>>();
            foreach (var service in services)
            {
                try
                {
                    execute(service);
                }
                catch (Exception ex)
                {
                    if (onError == null)
                        logger.LogError(ex, "Error executing delegate");
                    else
                        onError(ex);
                }
            }
        }


        public static Task RunDelegates<T>(this IServiceProvider services, Func<T, Task> execute, Action<Exception>? onError = null)
            => services.GetServices<T>().RunDelegates(execute, onError);


        public static async Task RunDelegates<T>(this IEnumerable<T> services, Func<T, Task> execute, Action<Exception>? onError = null)
        {
            if (services == null)
                return;

            var logger = ShinyHost.LoggerFactory.CreateLogger<T>();
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
                            logger.LogError(ex, "Error executing delegate");
                        else
                            onError(ex);
                    }
                })
                .ToList();

            await Task.WhenAll(tasks);
        }
    }
}
