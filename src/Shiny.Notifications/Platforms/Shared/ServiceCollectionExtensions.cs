using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Notifications;
using Shiny.Notifications.Infrastructure;
using Shiny.Stores;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <returns></returns>
    public static bool AddNotifications(this IServiceCollection services, Type? delegateType = null)
    {
#if IOS || MACCATALYST || ANDROID
        services.AddRepository<NotificationStoreConverter, Notification>();
        services.AddChannelManager();
        services.AddShinyService<NotificationManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);

#if ANDROID
        services.AddGeofencing<NotificationGeofenceDelegate>();
        services.AddSingleton<INotificationCustomizer, DefaultAndroidNotificationCustomizer>();
        services.TryAddSingleton<AndroidNotificationProcessor>();
        services.TryAddSingleton<AndroidNotificationManager>();
#endif
        return true;
#else
        return false;
#endif
    }


#if ANDROID
    public static bool AddNotifications(this IServiceCollection services, AndroidCustomizationOptions options, Type? delegateType)
    {
        services.AddSingleton(options);
        return services.AddNotifications(delegateType);
    }


    public static bool AddNotifications<T>(this IServiceCollection services, AndroidCustomizationOptions options) where T : INotificationDelegate
        => services.AddNotifications(options, typeof(T));

#endif

    public static bool AddNotifications<T>(this IServiceCollection services) where T : INotificationDelegate
        => services.AddNotifications(typeof(T));
}