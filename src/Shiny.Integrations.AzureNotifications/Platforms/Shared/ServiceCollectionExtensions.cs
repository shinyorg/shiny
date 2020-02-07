using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;
using Shiny.Push.AzureNotifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseAzureNotificationHubs<TPushDelegate>(this IServiceCollection services, string hubName, string listenerConnectionString)
             where TPushDelegate : class, IPushDelegate
        {
            
#if NETSTANDARD2_0
            return false;
#else
            services.AddSingleton(new AzureNotificationConfig(hubName, listenerConnectionString));
            services.AddSingleton<IPushDelegate, TPushDelegate>();
            services.AddSingleton<IPushManager, Shiny.Integrations.AzureNotifications.PushManager>();
            return true;
#endif
        }
    }
}
