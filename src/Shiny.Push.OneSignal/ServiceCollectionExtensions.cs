using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseOneSignalPush(this IServiceCollection services,
                                            Type delegateType,
                                            params NotificationCategory[] categories)
        {
#if NETSTANDARD2_0
            return false;
#else
            //services.RegisterModule(new PushModule(
            //    typeof(Shiny.Push.AzureNotificationHubs.PushManager),
            //    delegateType,
            //    categories
            //));
            //services.AddSingleton(new Shiny.Push.AzureNotificationHubs.AzureNotificationConfig(listenerConnectionString, hubName));
            return true;
#endif
        }


        public static bool UseOneSignalPush<TPushDelegate>(this IServiceCollection services,
                                                           params NotificationCategory[] categories)
            where TPushDelegate : class, IPushDelegate
            => services.UseOneSignalPush(
                typeof(TPushDelegate),
                categories
            );
    }
}
