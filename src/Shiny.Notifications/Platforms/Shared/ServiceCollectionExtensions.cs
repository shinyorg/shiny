using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Hosting;
using Shiny.Notifications;

namespace Shiny;


public static class ServiceCollectionExtensions
{
#if IOS || MACCATALYST
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services, IosConfiguration? configuration = null) where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate), configuration);


    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null, IosConfiguration? configuration = null)
    {
        services.AddSingleton(configuration ?? new());
        services.AddSingleton<NotificationManager>();
        services.AddSingleton<INotificationManager>(sp => sp.GetRequiredService<NotificationManager>());
        services.AddSingleton<IIosLifecycle.INotificationHandler>(sp => sp.GetRequiredService<NotificationManager>());

        services.AddDefaultRepository();
        services.AddJsonContext(ShinyNotificationsJsonContext.Default);
        if (!services.HasService<IChannelManager>())
        {
            services.AddSingleton<ChannelManager>();
            services.AddSingleton<IChannelManager>(sp => sp.GetRequiredService<ChannelManager>());
        }

        if (delegateType != null)
        {
            services.AddSingleton(delegateType);
            services.AddSingleton(typeof(INotificationDelegate), sp => sp.GetRequiredService(delegateType));
        }

        return services;
    }
#endif

#if MACOS

    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services, MacConfiguration? configuration = null) where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate), configuration);


    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null, MacConfiguration? configuration = null)
    {
        services.AddSingleton(configuration ?? new());
        services.AddSingleton<NotificationManager>();
        services.AddSingleton<INotificationManager>(sp => sp.GetRequiredService<NotificationManager>());
        services.AddSingleton<IMacLifecycle.INotificationHandler>(sp => sp.GetRequiredService<NotificationManager>());

        services.AddDefaultRepository();
        services.AddJsonContext(ShinyNotificationsJsonContext.Default);
        if (!services.HasService<IChannelManager>())
        {
            services.AddSingleton<ChannelManager>();
            services.AddSingleton<IChannelManager>(sp => sp.GetRequiredService<ChannelManager>());
        }

        if (delegateType != null)
        {
            services.AddSingleton(delegateType);
            services.AddSingleton(typeof(INotificationDelegate), sp => sp.GetRequiredService(delegateType));
        }

        return services;
    }
#endif

#if ANDROID

    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services) where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate));


    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null)
    {
        services.AddGeofencing<NotificationGeofenceDelegate>();
        services.TryAddSingleton<AndroidNotificationProcessor>();
        services.TryAddSingleton<AndroidNotificationManager>();
        services.AddSingleton<NotificationManager>();
        services.AddSingleton<INotificationManager>(sp => sp.GetRequiredService<NotificationManager>());
        services.AddSingleton<IAndroidLifecycle.IOnActivityOnCreate>(sp => sp.GetRequiredService<NotificationManager>());
        services.AddSingleton<IAndroidLifecycle.IOnActivityNewIntent>(sp => sp.GetRequiredService<NotificationManager>());

        services.AddDefaultRepository();
        services.AddJsonContext(ShinyNotificationsJsonContext.Default);
        if (!services.HasService<IChannelManager>())
        {
            services.AddSingleton<ChannelManager>();
            services.AddSingleton<IChannelManager>(sp => sp.GetRequiredService<ChannelManager>());
        }

        if (delegateType != null)
        {
            services.AddSingleton(delegateType);
            services.AddSingleton(typeof(INotificationDelegate), sp => sp.GetRequiredService(delegateType));
        }

        return services;
    }

#endif

#if WINDOWS

    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services) where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate));


    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null)
    {
        services.AddSingleton<NotificationManager>();
        services.AddSingleton<INotificationManager>(sp => sp.GetRequiredService<NotificationManager>());

        services.AddDefaultRepository();
        services.AddJsonContext(ShinyNotificationsJsonContext.Default);
        if (!services.HasService<IChannelManager>())
        {
            services.AddSingleton<ChannelManager>();
            services.AddSingleton<IChannelManager>(sp => sp.GetRequiredService<ChannelManager>());
        }

        if (delegateType != null)
        {
            services.AddSingleton(delegateType);
            services.AddSingleton(typeof(INotificationDelegate), sp => sp.GetRequiredService(delegateType));
        }

        return services;
    }

#endif
}
