using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Infrastructure.Impl;
using Shiny.Net;
using Shiny.Stores;
using Shiny.Stores.Impl;
using Shiny.Web.Infrastructure;
using Shiny.Web.Stores;

namespace Shiny;


public static class RegistrationExtensions
{
    public static WebAssemblyHostBuilder UseShiny(this WebAssemblyHostBuilder builder)
    {
        builder.RootComponents.Add<ShinyRootComponent>("head::after");
        builder.Services.TryAddSingleton<ISerializer, DefaultSerializer>();
        builder.Services.TryAddSingleton<IKeyValueStore, LocalStorageStore>();
        builder.Services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        builder.Services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        return builder;
    }


    public static IServiceCollection AddRepository<TStoreConverter, TEntity>(this IServiceCollection services)
            where TStoreConverter : class, IStoreConverter<TEntity>, new()
            where TEntity : IStoreEntity
    {
        services.AddSingleton<IRepository<TEntity>, LocalStorageRepository<TStoreConverter, TEntity>>();
        return services;
    }


    public static IServiceCollection AddConnectivity(this IServiceCollection services)
    {
        services.AddShinyService<Connectivity>();
        return services;
    }


    public static IServiceCollection AddBattery(this IServiceCollection services)
    {
        services.AddShinyService<Battery>();
        return services;
    }
}
