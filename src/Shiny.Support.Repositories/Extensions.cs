using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using Shiny.Support.Repositories;
using Shiny.Support.Repositories.Impl;

namespace Shiny;


/// <summary>
/// Service registration and convenience extensions for <see cref="IRepository"/>.
/// </summary>
public static class RepositoryExtensions
{
#if IOS || MACCATALYST || ANDROID || WINDOWS
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
    public static IServiceCollection AddDefaultRepository(this IServiceCollection services, DirectoryInfo? rootDirectory = null)
    {
        var dir = rootDirectory ?? new DirectoryInfo(Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Shiny"
        ));

        services.TryAddSingleton<IRepository>(sp => new FileSystemRepository(
            dir,
            sp.GetRequiredService<ISerializer>(),
            sp.GetRequiredService<ILogger<FileSystemRepository>>()
        ));
        return services;
    }
#endif

    /// <summary>
    /// Removes the given entity from the repository by its identifier.
    /// </summary>
    public static bool Remove<T>(this IRepository repository, T item) where T : IRepositoryEntity
        => repository.Remove<T>(item.Identifier);
}
