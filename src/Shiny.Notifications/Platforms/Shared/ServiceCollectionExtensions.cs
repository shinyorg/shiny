using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseNotifications(this IServiceCollection services,
                                            Type? delegateType,
                                            bool requestPermissionImmediately = false,
                                            AndroidOptions? androidConfig = null,
                                            UwpOptions? uwpConfig = null,
                                            params NotificationCategory[] notificationCategories)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new NotificationModule(
                delegateType,
                requestPermissionImmediately,
                androidConfig,
                uwpConfig,
                notificationCategories
            ));
            return true;
#endif
        }


        public static bool UseNotifications(this IServiceCollection services,
                                            Type? delegateType,
                                            bool requestPermissionImmediately = false,
                                            params NotificationCategory[] notificationCategories)
            => services.UseNotifications(delegateType, requestPermissionImmediately, notificationCategories);


        public static bool UseNotifications<TNotificationDelegate>(this IServiceCollection services,
                                                                   bool requestPermissionImmediately = false,
                                                                   params NotificationCategory[] notificationCategories)
                where TNotificationDelegate : class, INotificationDelegate
            => services.UseNotifications(
                typeof(TNotificationDelegate),
                requestPermissionImmediately,
                null,
                null,
                notificationCategories
            );


        public static bool UseNotifications<TNotificationDelegate>(this IServiceCollection services,
                                                                   bool requestPermissionImmediately = false,
                                                                   AndroidOptions? androidConfig = null,
                                                                   UwpOptions? uwpConfig = null,
                                                                   params NotificationCategory[] notificationCategories)
                where TNotificationDelegate : class, INotificationDelegate
            => services.UseNotifications(
                typeof(TNotificationDelegate),
                requestPermissionImmediately,
                androidConfig,
                uwpConfig,
                notificationCategories
            );


        public static bool UseNotifications(this IServiceCollection services,
                                            bool requestPermissionImmediately = false,
                                            AndroidOptions? androidConfig = null,
                                            UwpOptions? uwpConfig = null,
                                            params NotificationCategory[] notificationCategories)
        {
#if NETSTANDARD
            return false;
#else
#if __IOS__
            services.RegisterIosNotificationContext();
#endif
            services.RegisterModule(new NotificationModule(
                null,
                requestPermissionImmediately,
                androidConfig,
                uwpConfig,
                notificationCategories
            ));
            return true;
#endif
        }
    }
}