using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseAzureNotificationHubs(this IServiceCollection services, string hubName, string listenerConnectionString)
        {
            services.AddSingleton(new AzureNotificationConfig(hubName, listenerConnectionString))
            services.AddSingleton<Shiny.Push.IPushManager, Shiny.Integrations.AzureNotifications.PushManager>();
        }
    }
}
