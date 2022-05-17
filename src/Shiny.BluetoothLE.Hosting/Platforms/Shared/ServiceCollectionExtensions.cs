using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE.Hosting;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Registers the IPeripheralManager service that allows you to be a host BLE device
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddBluetoothLeHosting(this IServiceCollection services)
        {
#if IOS || ANDROID || MACCATALYST
            services.TryAddSingleton<IBleHostingManager, BleHostingManager>();
#else
            throw new InvalidOperationException("This platform is not supported");            
#endif
            return services;
        }
    }
}
