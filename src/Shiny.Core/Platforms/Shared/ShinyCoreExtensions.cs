#if PLATFORM
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


    public static IServiceCollection AddCommon(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        return services;
    }


    public static IServiceCollection AddRepository<TStoreConverter, TEntity>(this IServiceCollection services)
        where TStoreConverter : class, IStoreConverter<TEntity>, new()
        where TEntity : IStoreEntity
    {
        services.AddSingleton<IRepository<TEntity>, JsonFileRepository<TStoreConverter, TEntity>>();
        return services;
    }


    public static IServiceCollection AddBattery(this IServiceCollection services)
    {
        services.TryAddSingleton<IBattery, BatteryImpl>();
        return services;
    }


    public static IServiceCollection AddConnectivity(this IServiceCollection services)
    {
        services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
        return services;
    }
}
#endif