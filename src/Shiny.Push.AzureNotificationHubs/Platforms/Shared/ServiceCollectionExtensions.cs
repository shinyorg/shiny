using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;
using Shiny.Push.AzureNotificationHubs;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePushAzureNotificationHubs(this IServiceCollection services,
                                                        Type delegateType,
                                                        AzureNotificationConfig config)
        {
#if NETSTANDARD
            return false;
#else
            services.AddSingleton(config);
            services.RegisterModule(new PushModule(
                typeof(PushManager),
                delegateType
            ));
            return true;
#endif
        }


        public static bool UsePushAzureNotificationHubs(this IServiceCollection services,
                                                        Type delegateType,
                                                        string listenerConnectionString,
                                                        string hubName)
            => services.UsePushAzureNotificationHubs(delegateType, new AzureNotificationConfig(listenerConnectionString, hubName));


        public static bool UsePushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services,
                                                                       string listenerConnectionString,
                                                                       string hubName)
            where TPushDelegate : class, IPushDelegate
            => services.UsePushAzureNotificationHubs(
                typeof(TPushDelegate),
                listenerConnectionString,
                hubName
            );


        public static bool UsePushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services, AzureNotificationConfig config)
            => services.UsePushAzureNotificationHubs(typeof(TPushDelegate), config);
    }
}
