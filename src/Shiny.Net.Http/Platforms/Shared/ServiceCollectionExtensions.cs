using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Net.Http
{
    public static class ServiceCollectionExtensions
    {
        public static void UseHttpClientTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
            builder.TryAddStatefulSingleton<IHttpTransferDelegate, T>(nameof(IHttpTransferDelegate));
            builder.AddStartupSingleton<IHttpTransferManager, HttpClientHttpTransferManager>();
        }


        public static void UseHttpTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
#if NETSTANDARD
            builder.UseHttpClientTransfers<T>();
#elif WINDOWS_UWP || __IOS__
            builder.TryAddStatefulSingleton<IHttpTransferDelegate, T>(nameof(IHttpTransferDelegate));
            builder.AddStartupSingleton<IHttpTransferManager, HttpTransferManager>();
#elif __ANDROID__
            builder.TryAddStatefulSingleton<IHttpTransferDelegate, T>(nameof(IHttpTransferDelegate));
            builder.AddSingleton<IHttpTransferManager, HttpTransferManager>();
#endif
        }
    }
}
