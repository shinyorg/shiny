using System;
using System.IO;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using Shiny.Stores.Impl;
using Shiny.Support.Repositories;
using Shiny.Support.Repositories.Impl;

namespace Shiny;


public static class RepositoryExtensions
{
#if PLATFORM
    /// <summary>
    /// Registers the default JSON filesystem repository, stored in the platform's
    /// application data directory via <see cref="IPlatform"/>.
    /// </summary>
    public static IServiceCollection AddDefaultRepository(this IServiceCollection services)
    {
        services.TryAddSingleton<IRepository>(sp => new FileSystemRepository(
            sp.GetRequiredService<IPlatform>().AppData,
            sp.GetRequiredService<ISerializer>(),
            sp.GetRequiredService<ILogger<FileSystemRepository>>()
        ));
        return services;
    }
#else
    /// <summary>
    /// Registers the default JSON filesystem repository for plain .NET targets
    /// (Linux, macOS server, Blazor, etc.). Entities are serialized to disk
    /// using the same <c>{EntityName}_{Id}.shiny</c> convention as on iOS/Android.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="rootDirectory">
    /// Directory in which to store repository files. Defaults to
    /// <c>{LocalApplicationData}/Shiny</c>. Created if it does not already exist.
    /// </param>
    public static IServiceCollection AddDefaultRepository(this IServiceCollection services, DirectoryInfo? rootDirectory = null)
    {
        var dir = rootDirectory ?? new DirectoryInfo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Shiny"
        ));

        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IRepository>(sp => new FileSystemRepository(
            dir,
            sp.GetRequiredService<ISerializer>(),
            sp.GetRequiredService<ILogger<FileSystemRepository>>()
        ));
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
