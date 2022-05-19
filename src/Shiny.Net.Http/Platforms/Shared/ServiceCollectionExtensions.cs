using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Net.Http;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpTransfers(this IServiceCollection services, Type transferDelegateType)
    {
#if IOS || MACCATALYST || ANDROID
        services.TryAddSingleton<IHttpTransferManager, HttpTransferManager>();
        services.AddShinyService(typeof(IHttpTransferDelegate), transferDelegateType);
        services.AddRepository<HttpTransferStoreConverter, HttpTransfer>();
#endif
        return services;
    }


    public static IServiceCollection AddHttpTransfers<T>(this IServiceCollection services) where T : class, IHttpTransferDelegate
        => services.AddHttpTransfers(typeof(T));
}
