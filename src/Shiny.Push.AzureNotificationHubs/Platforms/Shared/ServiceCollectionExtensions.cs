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
}

