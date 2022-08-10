using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Notifications;
using Shiny.Notifications.Infrastructure;

namespace Shiny;


public static class NotificationExtensions
{
    public static INotificationManager Notifications(this ShinyContainer container) => container.GetService<INotificationManager>();


#if IOS || MACCATALYST
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static void AddNotifications<TDelegate>(this IServiceCollection services, IosConfiguration? configuration) where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate), configuration);


    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static void AddNotifications(this IServiceCollection services, Type? delegateType = null, IosConfiguration? configuration = null)
    {
        services.AddSingleton(configuration ?? new());
        services.AddRepository<NotificationStoreConverter, Notification>();
        services.AddChannelManager();
        services.AddShinyService<NotificationManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);
    }

#elif ANDROID

    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static void AddNotifications<TDelegate>(this IServiceCollection services, AndroidCustomizationOptions? options = null) where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate), options);


    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static void AddNotifications(this IServiceCollection services, Type? delegateType = null, AndroidCustomizationOptions? options = null)
    {
        services.AddSingleton(options ?? new());
        services.AddGeofencing<NotificationGeofenceDelegate>();
        //services.AddSingleton<INotificationCustomizer, DefaultAndroidNotificationCustomizer>();
        services.TryAddSingleton<AndroidNotificationProcessor>();
        services.TryAddSingleton<AndroidNotificationManager>();

        services.AddRepository<NotificationStoreConverter, Notification>();
        services.AddChannelManager();
        services.AddShinyService<NotificationManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);
    }

#endif
}