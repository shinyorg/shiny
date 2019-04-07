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

            // need to spin this up right away to get it going again - really only needed in iOS
            builder.RegisterPostBuildAction(c => c.GetService<IHttpTransferManager>());
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
                var del = services.GetService<IHttpTransferDelegate>();
                return new HttpTransferManager(repo, del, maxConnectionsPerHost);
            });

            // need to spin this up right away to get it going again
            builder.RegisterPostBuildAction(c => c.GetService<IHttpTransferManager>());
            return true;
        }
#endif
    }
}
