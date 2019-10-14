using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseNotifications(this IServiceCollection services,
                                            Type delegateType,
                                            bool requestPermissionImmediately = false,
                                            AndroidOptions androidConfig = null,
                                            UwpOptions uwpConfig = null)
        {
#if NETSTANDARD
            return false;
#else
            builder.RegisterModule(new NotificationModule(delegateType, requestPermissionImmediately, androidConfig, uwpConfig));
            return true;
#endif
        }


        public static bool UseNotifications<TNotificationDelegate>(this IServiceCollection services,
                                                                   bool requestPermissionImmediately = false,
                                                                   AndroidOptions androidConfig = null,
                                                                   UwpOptions uwpConfig = null)
                where TNotificationDelegate : class, INotificationDelegate
            => services.UseNotifications(
                typeof(TNotificationDelegate),
                requestPermissionImmediately,
                androidConfig,
                uwpConfig
            );

        public static bool UseNotifications(this IServiceCollection services,
                                            bool requestPermissionImmediately = false,
                                            AndroidOptions androidConfig = null,
                                            UwpOptions uwpConfig = null)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new NotificationModule(null, requestPermissionImmediately, androidConfig, uwpConfig));
            return true;
#endif
        }
    }
}