#if PLATFORM
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Http;
using Shiny.Stores;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IHttpTransferManager HttpTransfers(this ShinyContainer container) => container.GetService<IHttpTransferManager>();


    public static IServiceCollection AddHttpTransfers(this IServiceCollection services, Type transferDelegateType)
    {
        services.AddConnectivity();
        //services.AddShinyService<HttpTransferManager>();
        services.AddShinyService(transferDelegateType);
        services.AddRepository<HttpTransferStoreConverter, HttpTransfer>();
        return services;
    }


    public static IServiceCollection AddHttpTransfers<T>(this IServiceCollection services) where T : class, IHttpTransferDelegate
        => services.AddHttpTransfers(typeof(T));
}
#endif