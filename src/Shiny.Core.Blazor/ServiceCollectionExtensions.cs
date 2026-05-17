using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;
using Shiny.Storage.Blazor;
using Shiny.Stores;
using Shiny.Stores.Impl;
using Shiny.Support.Repositories;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConnectivity(this IServiceCollection services)
    {
        services.TryAddSingleton<IConnectivity, ConnectivityManager>();
        return services;
    }

    public static IServiceCollection AddBattery(this IServiceCollection services)
    {
        services.TryAddSingleton<IBattery, BatteryManager>();
        return services;
    }


    /// <summary>
    /// Registers a <see cref="IRepository"/> backed by the browser's localStorage.
    /// Consumers must include
    /// <c>&lt;script src="_content/Shiny.Core.Blazor/shiny-storage.js"&gt;&lt;/script&gt;</c>
    /// in their index.html before blazor.webassembly.js.
    /// </summary>
    public static IServiceCollection AddLocalStorageRepository(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IRepository, LocalStorageRepository>();
        return services;
    }


    /// <summary>
    /// Registers an <see cref="IKeyValueStore"/> backed by the browser's localStorage.
    /// Consumers must include
    /// <c>&lt;script src="_content/Shiny.Core.Blazor/shiny-storage.js"&gt;&lt;/script&gt;</c>
    /// in their index.html before blazor.webassembly.js.
    /// </summary>
    public static IServiceCollection AddLocalStorageKeyValueStore(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.AddKeyedSingleton<IKeyValueStore, LocalStorageKeyValueStore>(StoreKeys.Default);
        services.TryAddSingleton<IKeyValueStore>(
            sp => sp.GetRequiredKeyedService<IKeyValueStore>(StoreKeys.Default)
        );
        return services;
    }
}
