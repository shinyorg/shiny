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
                                                        string listenerConnectionString,
                                                        string hubName)
        {
#if NETSTANDARD
            return false;
#else
            services.AddSingleton(new AzureNotificationConfig(listenerConnectionString, hubName));
            services.RegisterModule(new PushModule(
                typeof(Shiny.Push.AzureNotificationHubs.PushManager),
                delegateType
            ));
            return true;
#endif
        }


        public static bool UsePushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services,
                                                                       string listenerConnectionString,
                                                                       string hubName)
            where TPushDelegate : class, IPushDelegate
            => services.UsePushAzureNotificationHubs(
                typeof(TPushDelegate),
                listenerConnectionString,
                hubName
            );
    }
}
