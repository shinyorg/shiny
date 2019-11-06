using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Integrations.XamEssentials;
using Shiny.Net;


namespace Shiny
{
    public static class ServicesCollectionExtensions
    {
#if __ANDROID__
        public static void Use(this IServiceCollection services)
        {

        }
#endif

        public static void UseXamEssentialsConnectivity(this IServiceCollection services)
        {
            services.AddSingleton<IConnectivity, EssentialsConnectivityImpl>();
        }
    }
}
