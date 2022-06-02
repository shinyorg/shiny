using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Stores;

namespace Shiny;


public static class ServiceExtensions
{
    /// <summary>
    /// Lazily resolves a service - helps in prevent resolve loops with delegates/services internal to Shiny
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="required"></param>
    /// <returns></returns>
    public static Lazy<T> GetLazyService<T>(this IServiceProvider services, bool required = false)
        => new Lazy<T>(() => required ? services.GetRequiredService<T>() : services.GetService<T>());


    /// <summary>
    /// This will add the implementation for ALL of its interfaces and create a persistent storage binding if INotifyPropertyChanged is implemented
    /// </summary>
    /// <typeparam name="TImpl"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyService<TImpl>(this IServiceCollection services) where TImpl : class
        => services.AddShinyService(typeof(TImpl));

    /// <summary>
    /// This will add the implementation for ALL of its interfaces and create a persistent storage binding if INotifyPropertyChanged is implemented
    /// </summary>
    /// <typeparam name="TImpl"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyService(this IServiceCollection services, Type implementationType)
    {
        var interfaces = implementationType.GetInterfaces();

        if (interfaces.Length == 0 || !interfaces.Any(x => x == typeof(INotifyPropertyChanged)))
        {
            services.AddSingleton(implementationType);
        }
        else
        {
            services.AddSingleton(implementationType, sp =>
            {
                var instance = (INotifyPropertyChanged)ActivatorUtilities.CreateInstance(sp, implementationType);
                sp.GetRequiredService<IObjectStoreBinder>().Bind(instance);
                return instance;
            });

            interfaces = interfaces.Where(x => x != typeof(INotifyPropertyChanged)).ToArray();
        }
        foreach (var iface in interfaces)
            services.AddSingleton(iface, sp => sp.GetRequiredService(implementationType));

        return services;
    }


    public static Task RunDelegates<T>(this IServiceProvider services, Func<T, Task> execute, Action<Exception>? onError = null)
        => services.GetServices<T>().RunDelegates(execute, onError);


    public static async Task RunDelegates<T>(this IEnumerable<T> services, Func<T, Task> execute, Action<Exception>? onError = null)
    {
        if (services == null)
            return;

        var logger = Host.Current.Logging.CreateLogger<T>();
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

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
