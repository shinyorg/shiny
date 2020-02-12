using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;
using Shiny.Push.AzureNotifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePushAzureNotificationHubs(this IServiceCollection services,
                                                        string hubName,
                                                        string listenerConnectionString,
                                                        bool requestAccessOnStart = false,
                                                        Type? delegateType = null)
        {
#if NETSTANDARD2_0
            return false;
#else
            if (services.UsePush(delegateType, requestAccessOnStart))
            {
                services.Remove(services.FirstOrDefault(x => x.ServiceType == typeof(IPushManager)));
                services.AddSingleton(new AzureNotificationConfig(hubName, listenerConnectionString));
                services.AddSingleton<IPushManager, Shiny.Integrations.AzureNotifications.PushManager>();
                return true;
            }
            return false;
#endif
        }


        public static bool UsePushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services,
                                                                       string hubName,
                                                                       string listenerConnectionString,
                                                                       bool requestAccessOnStart = false)
            where TPushDelegate : class, IPushDelegate
            => services.UsePushAzureNotificationHubs(hubName, listenerConnectionString, requestAccessOnStart, typeof(TPushDelegate));
    }
}
