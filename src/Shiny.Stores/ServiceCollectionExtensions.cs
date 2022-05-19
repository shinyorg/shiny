using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Stores;
using Shiny.Stores.Infrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepository<TStoreConverter, TEntity>(this IServiceCollection services)
        where TStoreConverter : class, IStoreConverter<TEntity>, new()
        where TEntity : IStoreEntity
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.AddSingleton<IRepository<TEntity>, JsonFileRepository<TStoreConverter, TEntity>>();
        return services;
    }
}
