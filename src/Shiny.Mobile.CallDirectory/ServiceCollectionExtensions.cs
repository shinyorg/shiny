using Microsoft.Extensions.DependencyInjection;
using Shiny.CallDirectory;

namespace Shiny;


/// <summary>Registers call directory services with the Shiny host.</summary>
public static class CallDirectoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICallDirectoryManager"/> - the iOS implementation on iOS, a no-op elsewhere.
    /// The iOS app also needs &lt;ShinyCallDirectoryExtension&gt;true&lt;/ShinyCallDirectoryExtension&gt; and
    /// &lt;ShinyCallDirectoryAppGroup&gt; in its project file, which build the extension.
    /// </summary>
    public static IServiceCollection AddCallDirectory(this IServiceCollection services, Action<CallDirectoryOptions>? configure = null)
    {
        var options = new CallDirectoryOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

#if PLATFORM
        services.AddSingleton<ICallDirectoryManager, CallDirectoryManager>();
#else
        services.AddSingleton<ICallDirectoryManager, NoOpCallDirectoryManager>();
#endif
        return services;
    }
}
