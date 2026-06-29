using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Hosting;
using Shiny.Net.Http;
using Shiny.Net.Http.Infrastructure;

namespace Shiny;


public static class HttpTransferServiceCollectionExtensions
{
    public static IServiceCollection AddHttpTransfers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate
    {
#if IOS || MACCATALYST
        services.AddSingletonAsImplementedInterfaces<HttpTransferManager>();
#elif ANDROID || WINDOWS
        services.AddSingletonAsImplementedInterfaces<HttpClientHttpTransferManager>();
        services.AddSingleton<HttpClientHttpTransferProcess>();
        services.AddHttpClient(HttpClientHttpTransferProcess.HttpClientName);
#endif
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        AddCore(services);

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
    public static IServiceCollection AddHttpClientTransfers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate
    {
        services.AddSingletonAsImplementedInterfaces<HttpClientHttpTransferManager>();
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        services.AddSingleton<HttpClientHttpTransferProcess>();
        services.AddHttpClient(HttpClientHttpTransferProcess.HttpClientName);

        AddCore(services);
        return services;
    }


    static void AddCore(IServiceCollection services)
    {
#if PLATFORM
        services.AddConnectivity();
#endif
        services.AddDefaultRepository();
        services.AddSingleton<HttpTransferMonitor>();
    }
}
