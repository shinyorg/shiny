using Shiny.Push;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Native Push Notification services without any background handling
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddPush(this IServiceCollection services)
    {
#if APPLE
        services.TryAddSingleton<IPushManager, PushManager>();
#endif
#if ANDROID
        services.AddPush(new FirebaseConfig());
#endif
        return services;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDelegate"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddPush<TDelegate>(this IServiceCollection services) where TDelegate : class, IPushDelegate
    {
        services.AddSingleton<IPushDelegate, TDelegate>();
        return services.AddPush();
    }

#if ANDROID

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddPush(this IServiceCollection services, FirebaseConfig config)
    {
        services.AddSingleton(config);
        services.TryAddSingleton<IPushManager, PushManager>();
        return services;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDelegate"></typeparam>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddPush<TDelegate>(this IServiceCollection services, FirebaseConfig config)
        where TDelegate : class, IPushDelegate
    {        
        services.AddSingleton<IPushDelegate, TDelegate>();
        services.AddPush(config);   
        return services;
    }
#endif
}
