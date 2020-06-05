using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePushAzureNotificationHubs(this IServiceCollection services,
                                                        Type delegateType,
                                                        string listenerConnectionString,
                                                        string hubName,
                                                        params NotificationCategory[] categories)
        {
#if NETSTANDARD2_0
            return false;
#else
            services.RegisterModule(new PushModule(
                typeof(Shiny.Push.AzureNotificationHubs.PushManager),
                delegateType,
                categories
            ));
            services.AddSingleton(new Shiny.Push.AzureNotificationHubs.AzureNotificationConfig(listenerConnectionString, hubName));
            return true;
#endif
        }


        public static bool UsePushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services,
                                                                       string listenerConnectionString,
                                                                       string hubName,
                                                                       params NotificationCategory[] categories)
            where TPushDelegate : class, IPushDelegate
            => services.UsePushAzureNotificationHubs(
                typeof(TPushDelegate),
                listenerConnectionString,
                hubName,
                categories
            );
    }
}
