using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Net.Http;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpTransfers(this IServiceCollection services, Type transferDelegateType)
    {
        services.AddShinyService(typeof(IHttpTransferDelegate), transferDelegateType);
        //services.TryAddSingleton<IHttpTransferManager, HttpClientHttpTransferManager>();
#if IOS || MACCATALYST || ANDROID
        services.TryAddSingleton<IHttpTransferManager, HttpTransferManager>();
#endif
        return services;
    }


    public static IServiceCollection AddHttpTransfers<T>(this IServiceCollection services) where T : class, IHttpTransferDelegate
        => services.AddHttpTransfers(typeof(T));
}
