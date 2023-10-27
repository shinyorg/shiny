#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Hosting;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Infrastructure;


public static class ShinyInfrastructureExtensions
{
    /// <summary>
    /// This is called by Shiny hosting - You should NOT be calling this yourself
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyCoreServices(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
#if ANDROID
        services.AddShinyService<AndroidPlatform>();
        services.AddShinyService<AndroidLifecycleExecutor>();
        services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
        services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
#elif APPLE
        services.AddShinyService<IosPlatform>();
        services.AddShinyService<IosLifecycleExecutor>();
        services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
        services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
#elif WINDOWS
        services.AddShinyService<WindowsPlatform>();
        services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
        services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
#endif
        return services;
    }
}
#endif