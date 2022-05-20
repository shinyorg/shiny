using Microsoft.Extensions.DependencyInjection;
using Shiny.Stores.Impl;

namespace Shiny.Stores;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepository<TStoreConverter, TEntity>(this IServiceCollection services)
        where TStoreConverter : class, IStoreConverter<TEntity>, new()
        where TEntity : IStoreEntity
    {
        services.AddSingleton<IRepository<TEntity>, JsonFileRepository<TStoreConverter, TEntity>>();
        return services;
    }
}
