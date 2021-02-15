using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Notifications;
using Shiny.Notifications.Logging;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add error logging using notifications - this should NEVER BE USED IN PRODUCTION
        /// </summary>
        /// <param name="builder"></param>
        public static void AddNotificationErrors(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, NotificationLoggerProvider>();
            builder.Services.UseNotifications(true);
        }


        public static bool UseNotifications(this IServiceCollection services,
                                            Type? delegateType,
                                            bool requestPermissionImmediately = false,
                                            AndroidOptions? androidConfig = null,
                                            params Channel[] channels)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new NotificationModule(
                delegateType,
                requestPermissionImmediately,
                androidConfig,
                channels
            ));
            return true;
#endif
        }


        public static bool UseNotifications<TNotificationDelegate>(this IServiceCollection services,
                                                                   bool requestPermissionImmediately = false,
                                                                   AndroidOptions? androidConfig = null,
                                                                   params Channel[] channels)
                where TNotificationDelegate : class, INotificationDelegate
            => services.UseNotifications(
                typeof(TNotificationDelegate),
                requestPermissionImmediately,
                androidConfig,
                channels
            );


        public static bool UseNotifications(this IServiceCollection services,
                                            bool requestPermissionImmediately = false,
                                            AndroidOptions? androidConfig = null,
                                            params Channel[] channels)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new NotificationModule(
                null,
                requestPermissionImmediately,
                androidConfig,
                channels
            ));
            return true;
#endif
        }
    }
}