#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;

namespace Shiny;


public static class NotificationsServiceCollectionExtensions
{
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {

        services.AddSingletonAsImplementedInterfaces<NotificationManager>();
        services.AddSingletonAsImplementedInterfaces<ChannelManager>();
        services.AddDefaultRepository();
        services.AddJsonContext(ShinyNotificationsJsonContext.Default);

        return services;
    }
    
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services) where TDelegate : class, INotificationDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        return services.AddNotifications();
    }
    
#if IOS || MACCATALYST
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services, IosConfiguration configuration) where TDelegate : class, INotificationDelegate
    {
        services.AddSingleton(configuration ?? new());
        return services.AddNotifications<TDelegate>();
    }

    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, IosConfiguration configuration)
    {
        services.AddSingleton(configuration ?? new());
        return services.AddNotifications();
    }
#endif
}
#endif