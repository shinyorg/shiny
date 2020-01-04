using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Nfc;


namespace Shiny
{
    public static class Extensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="services"></param>
        /// <param name="regions"></param>
        /// <returns></returns>
        public static bool UseNfc(this IServiceCollection services)
        {
#if NETSTANDARD
            return false;
#else
            services.AddSingleton<INfcManager, NfcManager>();
            return true;
#endif
        }

    }
}
