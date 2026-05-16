using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Shiny notifications backed by the freedesktop notifications D-Bus daemon.
    /// </summary>
    public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services)
        where TDelegate : INotificationDelegate
        => services.AddNotifications(typeof(TDelegate));


    /// <summary>
    /// Registers Shiny notifications backed by the freedesktop notifications D-Bus daemon.
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null)
    {
        services.AddSingleton<NotificationManager>();
        services.AddSingleton<INotificationManager>(sp => sp.GetRequiredService<NotificationManager>());

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
}
