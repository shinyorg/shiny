using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// This will attempt to resolve the service using standard TService, but if the TImpl implement a secondary service, it will add a registration in DI for that as well and resolve to the original main service
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImpl"></typeparam>
    /// <typeparam name="TOther"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection TryMultipleAddSingleton<TService, TImpl, TOther>(this IServiceCollection services)
        where TService : class
        where TImpl : class, TService
        where TOther : class
    {
        services.TryAddSingleton<TService, TImpl>();

        if (typeof(TImpl).IsAssignableTo(typeof(TOther)))
            services.AddSingleton(sp => (TOther)sp.GetRequiredService(typeof(TService)));

        return services;
    }


    /// <summary>
    /// This will wire up IShinyStartupTask if implemented and persistent storage binding if INotifyPropertyChanged implemented
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImpl"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyService<TService, TImpl>(this IServiceCollection services)
        where TService : class
        where TImpl : class, TService
        => services.AddShinyService(typeof(TService), typeof(TImpl));


    /// <summary>
    /// This will wire up IShinyStartupTask if implemented and persistent storage binding if INotifyPropertyChanged implemented
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceType"></param>
    /// <param name="implementationType"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyService(this IServiceCollection services, Type serviceType, Type implementationType)
    {
        if (implementationType.IsAssignableTo(typeof(INotifyPropertyChanged)))
        {
            services.AddSingleton(serviceType, sp =>
            {
                var instance = (INotifyPropertyChanged)ActivatorUtilities.CreateInstance(sp, implementationType);
                sp.GetRequiredService<IObjectStoreBinder>().Bind(instance);
                return instance;
            });
        }
        else
        {
            services.AddSingleton(serviceType, implementationType);
        }
        if (implementationType.IsAssignableTo(typeof(IShinyStartupTask)))
            services.AddSingleton<IShinyStartupTask>(sp => (IShinyStartupTask)sp.GetRequiredService(serviceType));

        return services;
    }


    /// <summary>
    /// This will wire up IShinyStartupTask if implemented and persistent storage binding if INotifyPropertyChanged implemented
    /// </summary>
    /// <param name="services"></param>
    /// <param name="implementationType"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyService(this IServiceCollection services, Type implementationType)
    {

        if (implementationType.IsAssignableTo(typeof(INotifyPropertyChanged)))
        {
            services.AddSingleton(implementationType, sp =>
            {
                var instance = (INotifyPropertyChanged)ActivatorUtilities.CreateInstance(sp, implementationType);
                sp.GetRequiredService<IObjectStoreBinder>().Bind(instance);
                return instance;
            });
        }
        else
        {
            services.AddSingleton(implementationType);
        }
        if (implementationType.IsAssignableTo(typeof(IShinyStartupTask)))
            services.AddSingleton<IShinyStartupTask>(sp => (IShinyStartupTask)sp.GetRequiredService(implementationType));

        return services;
    }


    /// <summary>
    /// This will wire up IShinyStartupTask if implemented and persistent storage binding if INotifyPropertyChanged implemented
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImpl"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyService<TImpl>(this IServiceCollection services) where TImpl : class
        => services.AddShinyService(typeof(TImpl));



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
