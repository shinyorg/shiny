#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Hosting;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Infrastructure;


public static class ShinyInfrastructureExtensions
{
    public static IServiceCollection AddShinyCoreServices(this IServiceCollection services)
    {
#if ANDROID
        services.AddShinyService<AndroidPlatform>();
        services.AddShinyService<AndroidLifecycleExecutor>();
        services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
        services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
        services.AddCommon();
#elif APPLE
        services.AddShinyService<IosPlatform>();
        services.AddShinyService<IosLifecycleExecutor>();
        services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
        services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
        services.AddCommon();
#endif
        return services;
    }


    internal static IServiceCollection AddCommon(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        return services;
    }
}
#endif