using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Nfc;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Attempts to register NFC services with Shiny
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static bool UseNfc(this IServiceCollection services)
        {
#if NETSTANDARD
            return false;
#else
            services.TryAddSingleton<INfcManager, NfcManager>();
            return true;
#endif
        }

    }
}
