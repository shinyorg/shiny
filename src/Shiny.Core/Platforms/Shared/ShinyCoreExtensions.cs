using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Hosting;
using Shiny.Infrastructure;
using Shiny.Infrastructure.Impl;
using Shiny.Net;
using Shiny.Power;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny;


public static class ShinyCoreExtensions
{
    public static void AddShinyCoreServices(this IServiceCollection services)
    {
#if ANDROID
        services.AddShinyService<AndroidPlatform>();
        services.AddShinyService<AndroidLifecycleExecutor>();
        services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
        services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
        services.AddCommon();
#elif IOS || MACCATALYST
        services.AddShinyService<IosPlatform>();
        services.AddShinyService<IosLifecycleExecutor>();
        services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
        services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
        services.AddCommon();
#endif
    }


    public static void AddCommon(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
    }


    public static IServiceCollection AddRepository<TStoreConverter, TEntity>(this IServiceCollection services)
        where TStoreConverter : class, IStoreConverter<TEntity>, new()
        where TEntity : IStoreEntity
    {
        services.AddSingleton<IRepository<TEntity>, JsonFileRepository<TStoreConverter, TEntity>>();
        return services;
    }


    public static bool AddBattery(this IServiceCollection services)
    {
#if IOS || MACCATALYST || ANDROID
        services.TryAddSingleton<IBattery, BatteryImpl>();
        return true;
#else
        return false;
#endif
    }


    public static bool AddConnectivity(this IServiceCollection services)
    {
#if IOS || MACCATALYST || ANDROID
        services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
        return true;
#else
        return false;
#endif
    }
}
