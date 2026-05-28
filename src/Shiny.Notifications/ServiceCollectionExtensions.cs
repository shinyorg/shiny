#if PLATFORM
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
#if IOS || MACCATALYST
        services.TryAddSingleton(new IosConfiguration());
#endif
        return services;
    }
    
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services) where TDelegate : class, INotificationDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        return services.AddNotifications();
    }
    
#if IOS || MACCATALYST
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services, IosConfiguration configuration) where TDelegate : class, INotificationDelegate
    {
        services.AddSingleton(configuration ?? new());
        return services.AddNotifications<TDelegate>();
    }

    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, IosConfiguration configuration)
    {
        services.AddSingleton(configuration);
        return services.AddNotifications();
    }
#endif
}
#endif