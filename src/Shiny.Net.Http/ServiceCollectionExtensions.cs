using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Hosting;
using Shiny.Net.Http;

namespace Shiny;


public static class HttpTransferServiceCollectionExtensions
{
    public static IServiceCollection AddHttpTransfers<TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate
    {
#if PLATFORM
        services.AddConnectivity();
#endif
        services.AddSingleton<HttpTransferManager>();
        services.AddSingleton<IHttpTransferManager>(sp => sp.GetRequiredService<HttpTransferManager>());
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<HttpTransferManager>());
#if IOS || MACCATALYST
        services.AddSingleton<IIosLifecycle.IHandleEventsForBackgroundUrl>(sp => sp.GetRequiredService<HttpTransferManager>());
#elif ANDROID || WINDOWS
        services.AddSingleton<AndroidHttpTransferProcess>();
#endif
        services.AddSingleton<TDelegate>();
        services.AddSingleton<IHttpTransferDelegate>(sp => sp.GetRequiredService<TDelegate>());
        services.AddDefaultRepository();
        services.AddJsonContext(ShinyHttpJsonContext.Default);
        services.AddSingleton<HttpTransferMonitor>();

        return services;
    }
    
    
    /// <summary>
    /// Registers the Shiny.Net.Http background transfer manager backed by a managed
    /// HttpClient + IConnectivity loop. Supports resumable downloads (uploads always
    /// restart). Use this on Linux, macOS server, Blazor, and other plain .NET targets.
    ///
    /// You must register an <see cref="Shiny.Net.IConnectivity"/> implementation
    /// (e.g. Shiny.Core.Linux or Shiny.Core.Blazor)
    /// before resolving services. A default JSON filesystem repository is
    /// registered automatically under {LocalApplicationData}/Shiny.
    /// </summary>
    public static IServiceCollection AddStandardHttpTransfers<TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate
    {
        services.AddSingleton<HttpTransferManager>();
        services.AddSingleton<IHttpTransferManager>(sp => sp.GetRequiredService<HttpTransferManager>());
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<HttpTransferManager>());

        services.AddSingleton<TDelegate>();
        services.AddSingleton<IHttpTransferDelegate>(sp => sp.GetRequiredService<TDelegate>());
        services.AddDefaultRepository();
        services.AddJsonContext(ShinyHttpJsonContext.Default);
        services.AddSingleton<HttpTransferMonitor>();
        services.AddSingleton<StandardHttpTransferProcess>();
        return services;
    }
}
