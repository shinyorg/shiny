#if PLATFORM
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Support.Repositories;
using Shiny.Support.Repositories.Impl;

namespace Shiny;


public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddRepository<TRepositoryConverter, TEntity>(this IServiceCollection services)
        where TRepositoryConverter : class, IRepositoryConverter<TEntity>, new()
        where TEntity : IRepositoryEntity
    {
        services.AddSingleton<IRepository<TEntity>, JsonFileRepository<TRepositoryConverter, TEntity>>();
        return services;
    }
}
#endif