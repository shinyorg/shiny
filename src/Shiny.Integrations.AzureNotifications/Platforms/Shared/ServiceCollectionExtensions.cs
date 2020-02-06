using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push.AzureNotifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseAzureNotificationHubs(this IServiceCollection services, string hubName, string listenerConnectionString)
        {
            services.AddSingleton(new AzureNotificationConfig(hubName, listenerConnectionString));
#if NETSTANDARD2_0
            return false;
#else
            services.AddSingleton<Shiny.Push.IPushManager, Shiny.Integrations.AzureNotifications.PushManager>();
            return true;
#endif
        }
    }
}
