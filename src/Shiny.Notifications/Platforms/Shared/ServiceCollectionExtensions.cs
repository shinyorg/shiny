using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        //public static void UseAppShutdownNotification(this IServiceCollection services, Notification notification)
        //    => services.AddAppState(sp =>
        //    {
        //        var manager = sp.GetRequiredService<INotificationManager>();
        //        return new NotificationAppStateDelegate(manager, notification);
        //    });


        public static bool UseNotifications(this IServiceCollection services,
                                            Type? delegateType,
                                            bool requestPermissionImmediately = false,
                                            AndroidOptions? androidConfig = null)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new NotificationModule(
                delegateType,
                requestPermissionImmediately,
                androidConfig
            ));
            return true;
#endif
        }


        public static bool UseNotifications(this IServiceCollection services,
                                            Type? delegateType,
                                            bool requestPermissionImmediately = false)
            => services.UseNotifications(delegateType, requestPermissionImmediately);


        public static bool UseNotifications<TNotificationDelegate>(this IServiceCollection services,
                                                                   bool requestPermissionImmediately = false)
                where TNotificationDelegate : class, INotificationDelegate
            => services.UseNotifications(
                typeof(TNotificationDelegate),
                requestPermissionImmediately,
                null
            );


        public static bool UseNotifications<TNotificationDelegate>(this IServiceCollection services,
                                                                   bool requestPermissionImmediately = false,
                                                                   AndroidOptions? androidConfig = null)
                where TNotificationDelegate : class, INotificationDelegate
            => services.UseNotifications(
                typeof(TNotificationDelegate),
                requestPermissionImmediately,
                androidConfig
            );


        public static bool UseNotifications(this IServiceCollection services,
                                            bool requestPermissionImmediately = false,
                                            AndroidOptions? androidConfig = null)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new NotificationModule(
                null,
                requestPermissionImmediately,
                androidConfig
            ));
            return true;
#endif
        }
    }
}