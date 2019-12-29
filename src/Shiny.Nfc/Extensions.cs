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
        public static bool UseNfc(this IServiceCollection services, Type nfcDelegateType)
        {
#if NETSTANDARD
            return false;
#else
            services.AddSingleton<INfcManager, NfcManager>();
            services.AddSingleton(typeof(INfcManager), nfcDelegateType);
            return true;
#endif
        }


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="regions"></param>
        /// <returns></returns>
        public static bool UseNfc<T>(this IServiceCollection services) where T : class, INfcDelegate
            => services.UseNfc(typeof(T));
    }
}
