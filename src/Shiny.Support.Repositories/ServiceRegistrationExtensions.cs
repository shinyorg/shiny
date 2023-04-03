#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Support.Repositories;
using Shiny.Support.Repositories.Impl;

namespace Shiny;


public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddDefaultRepository(this IServiceCollection services)
    {
        services.TryAddSingleton<IRepository, FileSystemRepository>();
        return services;
    }
}
#endif