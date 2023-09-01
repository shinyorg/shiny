using Shiny.Push;
//using Shiny.Notifications;
using Microsoft.Extensions.DependencyInjection;
#if ANDROID
using Microsoft.Extensions.DependencyInjection.Extensions;
#endif

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
        services.AddShinyService<PushManager>();
#elif ANDROID
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
        services.AddShinyService<TDelegate>();
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
        services.AddShinyService<PushManager>();
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
        services.AddShinyService<TDelegate>();
        services.AddPush(config);   
        return services;
    }
#endif
}
