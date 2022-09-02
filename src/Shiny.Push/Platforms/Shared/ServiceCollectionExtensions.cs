using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPush<TDelegate>(this IServiceCollection services) where TDelegate : class, IPushDelegate
        => services.AddPush(typeof(TDelegate));


    public static IServiceCollection AddPush(this IServiceCollection services, Type delegateType)
        => services.AddPush(typeof(PushManager), delegateType);


#if ANDROID
    public static IServiceCollection AddPush<TDelegate>(this IServiceCollection services, FirebaseConfig config) where TDelegate : class, IPushDelegate
        => services.AddPush(typeof(TDelegate), config);


    public static IServiceCollection AddPush(this IServiceCollection services, Type delegateType, FirebaseConfig config)
    {
        services.AddSingleton(config);
        services.AddPush(delegateType);
        return services;
    }
#endif
}
