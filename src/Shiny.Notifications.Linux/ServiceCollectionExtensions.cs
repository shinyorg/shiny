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
        services.AddShinyService<NotificationManager>();

        if (!services.HasService<IChannelManager>())
            services.AddShinyService<ChannelManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);

        return services;
    }
}
