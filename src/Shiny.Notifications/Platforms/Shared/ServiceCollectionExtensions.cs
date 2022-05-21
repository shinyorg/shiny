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
        services.AddShinyServiceWithLifecycle<INotificationManager, NotificationManager>();

        if (delegateType != null)
            services.AddShinyService(typeof(INotificationDelegate), delegateType);

#if ANDROID
        services.AddGeofencing<NotificationGeofenceDelegate>();
        services.TryAddSingleton<AndroidNotificationProcessor>();
        services.TryAddSingleton<AndroidNotificationManager>();
#endif
        return true;
#else
        return false;
#endif
    }


    public static bool AddNotifications<T>(this IServiceCollection services)
        => services.AddNotifications(typeof(T));
}