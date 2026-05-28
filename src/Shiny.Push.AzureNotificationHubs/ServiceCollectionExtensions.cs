using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;

namespace Shiny;


public static class AnhServiceCollectionExtensions
{
    static void AddCore(IServiceCollection services, string listenerConnectionString, string hubName)
    {
#if PLATFORM
        services.AddSingleton(new AzureNotificationConfig(listenerConnectionString, hubName));
        services.AddSingleton<AzureNotificationHubsPushProvider>();
#endif
    }
    
    public static IServiceCollection AddPushAzureNotificationHubs(this IServiceCollection services, string listenerConnectionString, string hubName)
    {
        services.AddPush();
        AddCore(services, listenerConnectionString, hubName);
        return services;
    }


    public static IServiceCollection AddPushAzureNotificationHubs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPushDelegate>(this IServiceCollection services, string listenerConnectionString, string hubName)
        where TPushDelegate : class, IPushDelegate
    {
        services.AddPush<TPushDelegate>();
        AddCore(services, listenerConnectionString, hubName);
        return services;
    }

#if ANDROID

    public static IServiceCollection AddPushAzureNotificationHubs(this IServiceCollection services, string listenerConnectionString, string hubName, FirebaseConfig firebaseConfig)
    {
        services.AddPush(firebaseConfig);
        AddCore(services, listenerConnectionString, hubName);
        return services;
    }


    public static IServiceCollection AddPushAzureNotificationHubs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TPushDelegate>(this IServiceCollection services, string listenerConnectionString, string hubName, FirebaseConfig firebaseConfig)
        where TPushDelegate : class, IPushDelegate
    {
        services.AddPush<TPushDelegate>(firebaseConfig);
        AddCore(services, listenerConnectionString, hubName);
        return services;
    }
#endif
}
