using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Http;

namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseHttpClientTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
            builder.AddSingleton<IHttpTransferDelegate, T>();
            builder.AddSingleton<IHttpTransferManager, HttpClientHttpTransferManager>();
        }


        public static void UseHttpTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
#if NETSTANDARD
            builder.UseHttpClientTransfers<T>();
#elif WINDOWS_UWP || __IOS__
            builder.AddSingleton<IHttpTransferDelegate, T>();
            builder.AddSingleton<IHttpTransferManager, HttpTransferManager>();
#elif __ANDROID__
            builder.AddSingleton<IHttpTransferDelegate, T>();
            builder.AddSingleton<IHttpTransferManager, HttpTransferManager>();
#endif
        }
    }
}
