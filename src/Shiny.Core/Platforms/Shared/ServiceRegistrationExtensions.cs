using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Net;
using Shiny.Power;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny;


public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddRepository<TStoreConverter, TEntity>(this IServiceCollection services)
            where TStoreConverter : class, IStoreConverter<TEntity>, new()
            where TEntity : IStoreEntity
    {
        services.AddSingleton<IRepository<TEntity>, JsonFileRepository<TStoreConverter, TEntity>>();
        return services;
    }
}

