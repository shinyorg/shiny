using System;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Support.Repositories;

namespace Shiny;


public static class RepositoryExtensions
{
    #if PLATFORM
    public static IServiceCollection AddDefaultRepository(this IServiceCollection services)
    {
        services.TryAddSingleton<IRepository, Shiny.Support.Repositories.Impl.FileSystemRepository>();
        return services;
    }

    #endif

    public static bool Remove<T>(this IRepository repository, T item) where T : IRepositoryEntity
        => repository.Remove<T>(item.Identifier);


    public static IObservable<int> CreateCountWatcher<T>(this IRepository repository) where T : IRepositoryEntity
    {
        var count = repository.GetList<T>().Count;

        return repository
            .WhenActionOccurs()
            .Where(x =>
                x.EntityType == typeof(T) &&
                x.Action != RepositoryAction.Update
            )
            .Select(x =>
            {
                switch (x.Action)
                {
                    case RepositoryAction.Add:
                        count++;
                        break;

                    case RepositoryAction.Remove:
                        count--;
                        break;

                    case RepositoryAction.Clear:
                        count = 0;
                        break;
                }

                return count;
            })
            .StartWith(count);
    }
}
