using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Stores;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection TryAddRepository(this IServiceCollection services)
    {
        services.TryAddSingleton<IRepository, SqliteRepository>();
        return services;
    }
}
