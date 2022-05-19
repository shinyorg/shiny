using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Net.Http;

namespace Shiny;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpTransfers(this IServiceCollection services, Type transferDelegateType)
    {
        services.AddJobs(); // TODO: only needed for httpclient version
        services.AddSingleton(typeof(IHttpTransferDelegate), transferDelegateType);
#if NETSTANDARD
        services.TryAddSingleton<IHttpTransferManager, HttpClientHttpTransferManager>();
#else
        services.TryAddSingleton<IHttpTransferManager, HttpTransferManager>();
#endif
        return services;
    }


    public static IServiceCollection AddHttpTransfers<T>(this IServiceCollection services) where T : class, IHttpTransferDelegate
        => services.AddHttpTransfers(typeof(T));
}
