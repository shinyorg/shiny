using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Net.Http
{
    public static class ServiceCollectionExtensions
    {
        public static void UseHttpClientTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
            builder.AddSingleton<IHttpTransferDelegate, T>();
            builder
                .AddSingleton<IHttpTransferManager, HttpClientHttpTransferManager>()
                .AsStartable<IHttpTransferManager>();
        }


        public static bool UseHttpTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
            builder.AddSingleton<IHttpTransferDelegate, T>();
#if NETSTANDARD
            builder.AddSingleton<IHttpTransferManager, HttpClientHttpTransferManager>();
#else
            builder.AddSingleton<IHttpTransferManager, HttpTransferManager>();
#endif

            // fire this guy up right away so any registrations get moving again - mostly needed for iOS
            builder.AsStartable<IHttpTransferManager>();
            return true;
        }


#if __IOS__
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="maxConnectionsPerHost"></param>
        /// <returns></returns>
        public static bool UseHttpTransfers<T>(this IServiceCollection builder, int maxConnectionsPerHost = 1) where T : class, IHttpTransferDelegate
        {
            builder.AddSingleton<IHttpTransferDelegate, T>();
            builder
                .AddSingleton<IHttpTransferManager>(services =>
                {
                    var del = services.GetService<IHttpTransferDelegate>();
                    return new HttpTransferManager(del, maxConnectionsPerHost);
                })
                .AsStartable<IHttpTransferManager>();
            return true;
        }
#endif
    }
}
