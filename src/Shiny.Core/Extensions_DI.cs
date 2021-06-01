using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;


namespace Shiny
{
    public static partial class Extensions
    {
        /// <summary>
        /// Register a module (like a category) of services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="module"></param>
        public static void RegisterModule(this IServiceCollection services, ShinyModule module)
        {
            module.Register(services);
            StartupModule.AddModule(module);
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
        public static T? Resolve<T>(this IServiceProvider serviceProvider, bool requiredService = false)
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
                services
                    .Resolve<ILogger<T>>()
                    .LogError(ex, "Error executing delegate");
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
