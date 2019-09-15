using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseNotifications<TNotificationDelegate>(this IServiceCollection builder,
                                                                   bool requestPermissionImmediately = false,
                                                                   AndroidOptions androidConfig = null,
                                                                   UwpOptions uwpConfig = null)
                where TNotificationDelegate : class, INotificationDelegate
        {
#if NETSTANDARD
            return false;
#else
            builder.RegisterModule(new NotificationModule(requestPermissionImmediately, androidConfig, uwpConfig));
            builder.AddSingleton<INotificationDelegate, TNotificationDelegate>();
            return true;
#endif
        }


        public static bool UseNotifications(this IServiceCollection builder,
                                            bool requestPermissionImmediately = false,
                                            AndroidOptions androidConfig = null,
                                            UwpOptions uwpConfig = null)
        {
#if NETSTANDARD
            return false;
#else
            builder.RegisterModule(new NotificationModule(requestPermissionImmediately, androidConfig, uwpConfig));
            return true;
#endif
        }
    }
}