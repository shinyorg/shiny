using Shiny.Push;
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
#if IOS || MACCATALYST
        services.AddSingleton<PushManager>();
        services.AddSingleton<IPushManager>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IApplePushManager>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IIosLifecycle.IOnFinishedLaunching>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IIosLifecycle.IRemoteNotifications>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IIosLifecycle.INotificationHandler>(sp => sp.GetRequiredService<PushManager>());
#elif MACOS
        services.AddSingleton<PushManager>();
        services.AddSingleton<IPushManager>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IApplePushManager>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IMacLifecycle.IOnFinishedLaunching>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IMacLifecycle.IRemoteNotifications>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IMacLifecycle.INotificationHandler>(sp => sp.GetRequiredService<PushManager>());
#elif ANDROID
        services.AddPush(new FirebaseConfig());
#elif WINDOWS
        services.AddSingleton<PushManager>();
        services.AddSingleton<IPushManager>(sp => sp.GetRequiredService<PushManager>());
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
        services.AddSingleton<TDelegate>();
        services.AddSingleton<IPushDelegate>(sp => sp.GetRequiredService<TDelegate>());
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
        services.AddSingleton<PushManager>();
        services.AddSingleton<IPushManager>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IAndroidLifecycle.IOnActivityOnCreate>(sp => sp.GetRequiredService<PushManager>());
        services.AddSingleton<IAndroidLifecycle.IOnActivityNewIntent>(sp => sp.GetRequiredService<PushManager>());
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
        services.AddSingleton<TDelegate>();
        services.AddSingleton<IPushDelegate>(sp => sp.GetRequiredService<TDelegate>());
        services.AddPush(config);
        return services;
    }
#endif
}
