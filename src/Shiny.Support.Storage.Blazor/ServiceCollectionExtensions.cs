using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Storage.Blazor;
using Shiny.Stores;
using Shiny.Stores.Impl;
using Shiny.Support.Repositories;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="IRepository"/> backed by the browser's localStorage.
    /// Consumers must include
    /// <c>&lt;script src="_content/Shiny.Support.Storage.Blazor/shiny-storage.js"&gt;&lt;/script&gt;</c>
    /// in their index.html before blazor.webassembly.js.
    /// </summary>
    public static IServiceCollection AddLocalStorageRepository(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IRepository, LocalStorageRepository>();
        return services;
    }


    /// <summary>
    /// Registers an <see cref="IKeyValueStore"/> backed by the browser's localStorage along
    /// with the <see cref="IKeyValueStoreFactory"/> and <see cref="IObjectStoreBinder"/>.
    /// Consumers must include
    /// <c>&lt;script src="_content/Shiny.Support.Storage.Blazor/shiny-storage.js"&gt;&lt;/script&gt;</c>
    /// in their index.html before blazor.webassembly.js.
    /// </summary>
    public static IServiceCollection AddLocalStorageKeyValueStore(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        services.AddSingleton<IKeyValueStore, LocalStorageKeyValueStore>();
        return services;
    }
}
