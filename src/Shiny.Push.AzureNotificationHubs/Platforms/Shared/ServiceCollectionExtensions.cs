using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;
using Shiny.Push.AzureNotificationHubs;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool AddPushAzureNotificationHubs(this IServiceCollection services,
                                                        Type delegateType,
                                                        AzureNotificationConfig config)
        {
#if NETSTANDARD
            return false;
#else
            services.AddSingleton(config);
            services.AddPush(typeof(Shiny.Push.AzureNotificationHubs.PushManager), delegateType);
            return true;
#endif
        }


        public static bool AddPushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services, AzureNotificationConfig config)
            where TPushDelegate : class, IPushDelegate
            => services.AddPushAzureNotificationHubs(
                typeof(TPushDelegate),
                config
            );
    }
}
