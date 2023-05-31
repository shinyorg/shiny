using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Http;

namespace Shiny;


public static class ServiceCollectionExtensions
{
#if APPLE
    public static IServiceCollection AddHttpTransfers<TDelegate>(this IServiceCollection services, AppleConfiguration? config = null)
#else
    public static IServiceCollection AddHttpTransfers<TDelegate>(this IServiceCollection services)
#endif
        where TDelegate : class, IHttpTransferDelegate
    {
        services.AddConnectivity();

        services.AddShinyService<HttpTransferManager>();
        services.AddShinyService(typeof(TDelegate));
        services.AddDefaultRepository();
        services.AddSingleton<HttpTransferMonitor>();
        
#if ANDROID
        services.AddJob(typeof(TransferJob), requiredNetwork: Jobs.InternetAccess.Any, runInForeground: true);
#else
        services.AddSingleton(config ?? new AppleConfiguration());
#endif
        return services;
    }
}