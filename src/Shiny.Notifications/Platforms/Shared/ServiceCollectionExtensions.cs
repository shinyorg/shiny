using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Notifications
{
    public static class ContainerBuilderExtensions
    {
        public static bool UseNotifications(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<INotificationManager, NotificationManagerImpl>();
            return true;
#endif
        }
    }
}
