using System;
using Microsoft.Extensions.DependencyInjection;
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
            builder.AddSingleton<IBleHostingManager, BleHostingManager>();
            return true;
#endif
        }
    }
}
