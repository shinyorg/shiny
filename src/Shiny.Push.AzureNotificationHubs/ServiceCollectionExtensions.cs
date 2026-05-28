using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;

namespace Shiny;


public static class AnhServiceCollectionExtensions
{
    public static IServiceCollection AddPushAzureNotificationHubs(this IServiceCollection services, string listenerConnectionString, string hubName)
    {
        services.AddPush();
        services.AddSingleton(new AzureNotificationConfig(listenerConnectionString, hubName));
        services.AddSingleton<AzureNotificationHubsPushProvider>();
        return services;
    }


    public static IServiceCollection AddPushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services, string listenerConnectionString, string hubName)
        where TPushDelegate : class, IPushDelegate
    {
        services.AddSingleton<TPushDelegate>();
        services.AddPushAzureNotificationHubs(listenerConnectionString, hubName);
        return services;
    }

#if ANDROID

    public static IServiceCollection AddPushAzureNotificationHubs(this IServiceCollection services, string listenerConnectionString, string hubName, FirebaseConfig firebaseConfig)
    {
        services.AddPush(firebaseConfig);
        services.AddSingleton(new AzureNotificationConfig(listenerConnectionString, hubName));
        services.AddSingleton<AzureNotificationHubsPushProvider>();
        services.AddSingleton<IPushProvider>(sp => sp.GetRequiredService<AzureNotificationHubsPushProvider>());
        services.AddSingleton<IPushTagSupport>(sp => sp.GetRequiredService<AzureNotificationHubsPushProvider>());
        return services;
    }


    public static IServiceCollection AddPushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services, string listenerConnectionString, string hubName, FirebaseConfig firebaseConfig)
        where TPushDelegate : class, IPushDelegate
    {
        services.AddPush<TPushDelegate>(firebaseConfig);
        services.AddSingleton(new AzureNotificationConfig(listenerConnectionString, hubName));
        services.AddSingleton<AzureNotificationHubsPushProvider>();
        services.AddSingleton<IPushProvider>(sp => sp.GetRequiredService<AzureNotificationHubsPushProvider>());
        services.AddSingleton<IPushTagSupport>(sp => sp.GetRequiredService<AzureNotificationHubsPushProvider>());
        return services;
    }
#endif
}
