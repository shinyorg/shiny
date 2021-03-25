using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers notification manager with Shiny
        /// </summary>
        /// <param name="services"></param>
        /// <param name="delegateType"></param>
        /// <param name="androidConfig">Android specific default configuration</param>
        /// <param name="channels">WARNING: This will replace all current channels with this set</param>
        /// <returns></returns>
        public static bool UseNotifications(this IServiceCollection services,
                                            Type? delegateType,
                                            AndroidOptions? androidConfig = null,
                                            params Channel[] channels)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new NotificationModule(
                delegateType,
                androidConfig,
                channels
            ));
            return true;
#endif
        }


        /// <summary>
        /// Registers notification manager with Shiny
        /// </summary>
        /// <typeparam name="TNotificationDelegate"></typeparam>
        /// <param name="services"></param>
        /// <param name="androidConfig">Android specific default configuration</param>
        /// <param name="channels">WARNING: This will replace all current channels with this set</param>
        /// <returns></returns>
        public static bool UseNotifications<TNotificationDelegate>(this IServiceCollection services,
                                                                   AndroidOptions? androidConfig = null,
                                                                   params Channel[] channels)
                where TNotificationDelegate : class, INotificationDelegate
            => services.UseNotifications(
                typeof(TNotificationDelegate),
                androidConfig,
                channels
            );


        /// <summary>
        /// Registers notification manager with Shiny
        /// </summary>
        /// <param name="services"></param>
        /// <param name="androidConfig">Android specific default configuration</param>
        /// <param name="channels">WARNING: This will replace all current channels with this set</param>
        /// <returns></returns>
        public static bool UseNotifications(this IServiceCollection services,
                                            AndroidOptions? androidConfig = null,
                                            params Channel[] channels)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new NotificationModule(
                null,
                androidConfig,
                channels
            ));
            return true;
#endif
        }
    }
}