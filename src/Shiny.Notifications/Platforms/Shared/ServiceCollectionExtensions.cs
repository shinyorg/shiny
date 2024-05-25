using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Notifications;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services) where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate));


#if !ANDROID
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null)
    {
        services.AddShinyService<NotificationManager>();

        services.AddDefaultRepository();
        if (!services.HasService<IChannelManager>())
            services.AddShinyService<ChannelManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);

        return services;
    }
#else

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
        services.AddShinyService<NotificationManager>();

        services.AddDefaultRepository();
        if (!services.HasService<IChannelManager>())
            services.AddShinyService<ChannelManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);

        return services;
    }

#endif
}