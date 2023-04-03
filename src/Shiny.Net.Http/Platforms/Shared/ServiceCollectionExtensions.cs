using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Http;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpTransfers<TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate
    {
        services.AddShinyService<HttpTransferManager>();
        services.AddShinyService(typeof(TDelegate));

#if ANDROID
        services.AddJob(typeof(TransferJob), requiredNetwork: Jobs.InternetAccess.Any, runInForeground: true);
        services.AddDefaultRepository();
#endif
        return services;
    }
}