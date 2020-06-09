using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Vpn;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseVpn(this IServiceCollection services)
        {
#if NETSTANDARD2_0
            return false;
#else
            services.AddSingleton<IVpnManager, VpnManager>();
            return true;
#endif
        }
    }
}
