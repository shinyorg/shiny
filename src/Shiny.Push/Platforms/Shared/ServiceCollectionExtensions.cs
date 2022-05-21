using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static bool AddPush<TDelegate>(this IServiceCollection services) where TDelegate : class, IPushDelegate
        => services.AddPush(typeof(TDelegate));


    public static bool AddPush(this IServiceCollection services, Type delegateType)
        => services.AddPush(typeof(PushManager), delegateType);


#if ANDROID
    public static bool AddPush<TDelegate>(this IServiceCollection services, FirebaseConfig config) where TDelegate : class, IPushDelegate
        => services.UsePush(typeof(TDelegate), config);


    public static bool UsePush(this IServiceCollection services, Type delegateType, FirebaseConfig config)
    {
        services.AddSingleton(config);
        services.AddPush(delegateType);
        return true;
    }
#endif


    
}
