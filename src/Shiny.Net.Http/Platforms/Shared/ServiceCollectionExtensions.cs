using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Http;

namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseHttpTransfers(this IServiceCollection services, Type transferDelegateType)
        {
            services.AddSingleton(typeof(IHttpTransferDelegate), transferDelegateType);
#if NETSTANDARD
            services.AddSingleton<IHttpTransferManager, HttpClientHttpTransferManager>();
#elif WINDOWS_UWP || __IOS__
            services.AddSingleton<IHttpTransferManager, HttpTransferManager>();
#elif __ANDROID__
            services.AddSingleton<IHttpTransferManager, HttpTransferManager>();
#endif
        }


        public static void UseHttpTransfers<T>(this IServiceCollection services) where T : class, IHttpTransferDelegate
            => services.UseHttpTransfers(typeof(T));
    }
}
