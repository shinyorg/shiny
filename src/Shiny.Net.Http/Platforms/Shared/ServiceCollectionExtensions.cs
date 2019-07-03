using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Net.Http
{
    public static class ServiceCollectionExtensions
    {
        public static void UseHttpClientTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
            builder.AddService<IHttpTransferDelegate, T>();
            builder.AddService<IHttpTransferManager, HttpClientHttpTransferManager>();
        }


        public static void UseHttpTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
#if NETSTANDARD
            builder.UseHttpClientTransfers<T>();
#elif WINDOWS_UWP || __IOS__
            builder.AddService<IHttpTransferDelegate, T>();
            builder.AddService<IHttpTransferManager, HttpTransferManager>();
#elif __ANDROID__
            builder.AddService<IHttpTransferDelegate, T>();
            builder.AddService<IHttpTransferManager, HttpTransferManager>();
#endif
        }
    }
}
