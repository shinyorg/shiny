using Shiny.Push;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Hosting;
#if ANDROID
using Microsoft.Extensions.DependencyInjection.Extensions;
#endif

namespace Shiny;


public static class PushServiceCollectionExtensions
{
    /// <summary>
    /// Adds Native Push Notification services without any background handling
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddPush(this IServiceCollection services)
    {
#if PLATFORM
        services.AddSingletonAsImplementedInterfaces<PushManager>();
#endif
#if ANDROID
        services.AddPush(new FirebaseConfig());
#endif
        return services;
    }


    /// <summary>
    /// Add push notifications with delegate
    /// </summary>
    /// <typeparam name="TDelegate"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddPush<TDelegate>(this IServiceCollection services) where TDelegate : class, IPushDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
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
        services.AddSingletonAsImplementedInterfaces<PushManager>();
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
        services.AddPush<TDelegate>();
        services.AddPush(config);
        return services;
    }
#endif
}
