using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Logging;


namespace Shiny
{



    public static class ServiceExtensions
    {
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
    }
}
