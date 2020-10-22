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
#elif WINDOWS_UWP || __ANDROID__
            services.TryAddSingleton<IHttpTransferManager, HttpTransferManager>();
#elif __IOS__
            services.AddSingleton<IHttpTransferManager, HttpTransferManager>();
            services.AddSingleton(sp => (IAppDelegateBackgroundUrlHandler)sp.Resolve<IHttpTransferManager>());
#endif
        }


        public static void UseHttpTransfers<T>(this IServiceCollection services) where T : class, IHttpTransferDelegate
            => services.UseHttpTransfers(typeof(T));
    }
}
