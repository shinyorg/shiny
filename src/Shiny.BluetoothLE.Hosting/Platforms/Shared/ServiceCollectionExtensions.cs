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
        public static bool UseBleHosting(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#else
            builder.TryAddSingleton<IBleHostingManager, BleHostingManager>();
            return true;
#endif
        }
    }
}
