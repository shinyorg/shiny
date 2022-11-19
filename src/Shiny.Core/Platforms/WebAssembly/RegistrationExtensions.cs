using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Infrastructure.Impl;
using Shiny.Net;
using Shiny.Stores;
using Shiny.Stores.Impl;
using Shiny.Web.Stores;

namespace Shiny;


public static class RegistrationExtensions
{
    public static IServiceCollection UseShiny(this IServiceCollection services)
    {
        if (!OperatingSystem.IsBrowser())
            throw new InvalidProgramException("You are not within a browser environment to be able to load webassembly");

        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        services.AddShinyService<LocalStorageStore>();

        return services;
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
