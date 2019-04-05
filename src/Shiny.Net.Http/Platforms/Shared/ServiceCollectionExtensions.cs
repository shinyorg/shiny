using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny.Net.Http
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseHttpTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
            builder.AddSingleton<IHttpTransferDelegate, T>();
            builder.AddSingleton<IHttpTransferManager, HttpTransferManager>();
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
            builder.AddSingleton<IHttpTransferManager>(services =>
            {
                var repo = services.GetService<IRepository>();
                //return new HttpTransferManager(repo, maxConnectionsPerHost);
                return null; // TODO
            });
            return true;
        }
#endif
    }
}
