
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shiny.Infrastructure;
using Shiny.Infrastructure.Impl;
using Shiny.Stores;
using Shiny.Web.Infrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static void UseShiny(this IServiceCollection services)
    {
        //services.TryAddSingleton<IRepository, IndexDbRepository>();
        //services.TryAddSingleton<IConnectivity, Connectivity>();
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        //services.TryAddSingleton<IKeyValueStore, LocalStorageStore>();
        //services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        //services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
    }


    public static void AddConnectivity(this IServiceCollection services)
    {

    }


    public static void AddBattery(this IServiceCollection services)
    {

    }
}
