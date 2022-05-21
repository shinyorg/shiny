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
#if IOS || MACCATALYST || ANDROID
        services.AddShinyServiceWithLifecycle<IHttpTransferManager, HttpTransferManager>();
        services.AddShinyService(typeof(IHttpTransferDelegate), transferDelegateType);
        services.AddRepository<HttpTransferStoreConverter, HttpTransfer>();
#endif
        return services;
    }


    public static IServiceCollection AddHttpTransfers<T>(this IServiceCollection services) where T : class, IHttpTransferDelegate
        => services.AddHttpTransfers(typeof(T));
}
