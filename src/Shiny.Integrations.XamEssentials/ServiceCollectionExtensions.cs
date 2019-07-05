using System;
using Microsoft.Extensions.DependencyInjection;


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

        }
    }
}
