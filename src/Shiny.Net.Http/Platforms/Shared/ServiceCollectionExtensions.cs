using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Net.Http
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseHttpTransfers<T>(this IServiceCollection builder) where T : class, IHttpTransferDelegate
        {
            builder.AddSingleton<IHttpTransferDelegate, T>();
            builder.AddSingleton<IHttpTransferManager, HttpTransferManager>();

            // fire this guy up right away so any registrations get moving again - mostly needed for iOS
            builder.RegisterPostBuildAction(sp => sp.GetService<IHttpTransferManager>());
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
                var del = services.GetService<IHttpTransferDelegate>();
                return new HttpTransferManager(del, maxConnectionsPerHost);
            });

            // need to spin this up right away to get it going again
            builder.RegisterPostBuildAction(c => c.GetService<IHttpTransferManager>());
            return true;
        }
#endif
    }
}
