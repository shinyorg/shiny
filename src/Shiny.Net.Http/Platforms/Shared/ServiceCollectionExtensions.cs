using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Net.Http;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseHttpTransfers(this IServiceCollection services, Type transferDelegateType)
        {
            services.AddSingleton(typeof(IHttpTransferDelegate), transferDelegateType);
#if NETSTANDARD
            services.TryAddSingleton<IHttpTransferManager, HttpClientHttpTransferManager>();
#else
            services.TryAddSingleton<IHttpTransferManager, HttpTransferManager>();
            services.TryAddSingleton<IHttpTransferManager, HttpTransferManager>();
#endif
        }


        public static void UseHttpTransfers<T>(this IServiceCollection services) where T : class, IHttpTransferDelegate
            => services.UseHttpTransfers(typeof(T));
    }
}
