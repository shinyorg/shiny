using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Stores;
using Shiny.Stores.Infrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection TryAddRepository(this IServiceCollection services)
    {
        //services.TryAddSingleton<IRepository, SqliteRepository>();
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IRepository, JsonFileRepository>();
        return services;
    }
}
