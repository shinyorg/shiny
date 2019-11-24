using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePushNotifications<TDelegate>(this IServiceCollection services) where TDelegate : class, IPushNotificationDelegate
            => services.UsePushNotifications(typeof(TDelegate));


        public static bool UsePushNotifications(this IServiceCollection services, Type delegateType)
        {
#if NETSTANDARD
            return false;
#else
            services.AddSingleton<IPushNotificationManager, PushNotificationManager>();
            services.AddSingleton(typeof(IPushNotificationDelegate), delegateType);
            return true;
#endif
        }
    }
}
