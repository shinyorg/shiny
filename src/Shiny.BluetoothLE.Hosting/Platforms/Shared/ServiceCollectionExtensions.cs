using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE.Hosting;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static IBleHostingManager BleHosting(this ShinyContainer container) => container.GetService<IBleHostingManager>();


        /// <summary>
        /// Registers the IPeripheralManager service that allows you to be a host BLE device
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static bool AddBluetoothLeHosting(this IServiceCollection services)
        {
#if IOS || ANDROID || MACCATALYST
            services.TryAddSingleton<IBleHostingManager, BleHostingManager>();
            return true;
#else
            return false;
#endif
        }
    }
}
