using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Notifications;
using Shiny.Notifications.Infrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
#if IOS || MACCATALYST
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services, IosConfiguration? configuration = null) where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate), configuration);


    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null, IosConfiguration? configuration = null)
    {
        services.AddSingleton(configuration ?? new());
        services.AddRepository<NotificationStoreConverter, Notification>();
        services.AddChannelManager();
        services.AddShinyService<NotificationManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);

        return services;
    }
#endif

#if ANDROID

    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services) where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate));


    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null)
    {
        services.AddGeofencing<NotificationGeofenceDelegate>();
        services.TryAddSingleton<AndroidNotificationProcessor>();
        services.TryAddSingleton<AndroidNotificationManager>();

        services.AddRepository<NotificationStoreConverter, Notification>();
        services.AddChannelManager();
        services.AddShinyService<NotificationManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);

        return services;
    }

#endif
}