using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Logging;

namespace Shiny;


public static class ServiceExtensions
{
    /// <summary>
    /// Add the debug logger - this will only output if the debugger is attached
    /// </summary>
    /// <param name="builder"></param>
    public static void AddDebug(this ILoggingBuilder builder)
        => builder.AddProvider(new DebugLoggerProvider());


    /// <summary>
    /// Add the console logger - this is also used if you have not provided a logging provider to shiny
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="logLevel"></param>
    public static void AddConsole(this ILoggingBuilder builder, LogLevel logLevel = LogLevel.Warning)
        => builder.AddProvider(new ConsoleLoggerProvider(logLevel));


    /// <summary>
    /// This will attempt to resolve the service using standard TService, but if the TImpl implement a secondary service, it will add a registration in DI for that as well and resolve to the original main service
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImpl"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection TryMultipleAddSingleton<TService, TImpl, TOther>(this IServiceCollection services)
        where TService : class
        where TImpl : class, TService
        where TOther : class
    {
        services.TryAddSingleton<TService, TImpl>();

        if (typeof(TImpl).IsAssignableTo(typeof(TOther)))
            services.AddSingleton<TOther>(sp => (TOther)sp.GetRequiredService(typeof(TService)));

        return services;
    }


    /// <summary>
    /// This will wire IShinyStartupTask and persistent storage binding (if you model implement INotifyPropertyChanged)
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImpl"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyService<TService, TImpl>(this IServiceCollection services)
        where TService : class
        where TImpl : class, TService
    {
        if (typeof(TImpl).IsAssignableTo(typeof(INotifyPropertyChanged)))
        {
            services.AddSingleton(sp =>
            {
                var instance = (TService)ActivatorUtilities.CreateInstance(sp, typeof(TImpl));
                // TODO: bind object
                return instance;
            });
        }
        else
        {
            services.AddSingleton<TService, TImpl>();
        }
        services.TryMultipleAddSingleton<TService, TImpl, IShinyStartupTask>();

        return services;
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    public static async Task SafeResolveAndExecute<T>(this IServiceProvider services, Func<T, Task> execute)
    {
        try
        {
            var service = services.GetService<T>();
            if (service != null)
                await execute.Invoke(service).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            services
                .GetRequiredService<ILogger<T>>()
                .LogError(ex, "Error executing delegate");
        }
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

        await Task.WhenAll(tasks);
    }
}
