using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Http;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Shiny.Net.Http background transfer manager backed by a managed
    /// HttpClient + IConnectivity loop. Supports resumable downloads (uploads always
    /// restart). Use this on Linux, macOS server, Blazor, and other plain .NET targets.
    ///
    /// You must register an <see cref="Shiny.Net.IConnectivity"/> implementation
    /// (e.g. Shiny.Support.DeviceMonitoring.Linux or Shiny.Support.DeviceMonitoring.Blazor)
    /// before resolving services. A default JSON filesystem repository is
    /// registered automatically under {LocalApplicationData}/Shiny.
    /// </summary>
    public static IServiceCollection AddStandardHttpTransfers<TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate
    {
        services.AddShinyService<HttpTransferManager>();
        services.AddShinyService(typeof(TDelegate));
        services.AddDefaultRepository();
        services.AddSingleton<HttpTransferMonitor>();
        services.AddSingleton<HttpTransferProcess>();
        return services;
    }
}
