using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Stores;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection TryAddSqliteRepository(this IServiceCollection services)
    {
        services.TryAddSingleton<IRepository, SqliteRepository>();
        return services;
    }
}
