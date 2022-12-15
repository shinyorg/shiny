using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPushAzureNotificationHubs(this IServiceCollection services, string listenerConnectionString, string hubName)
    {
        services.AddPush();
        services.AddSingleton(new AzureNotificationConfig(listenerConnectionString, hubName));
        services.AddShinyService<AzureNotificationHubsPushProvider>();
        return services;
    }


    public static IServiceCollection AddPushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services, string listenerConnectionString, string hubName)
        where TPushDelegate : class, IPushDelegate
    {
        services.AddSingleton<IPushDelegate, TPushDelegate>();
        services.AddPushAzureNotificationHubs(listenerConnectionString, hubName);
        return services;
    }
}