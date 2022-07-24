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


        // TODO: could do auto start (or auto restart) but this is dangerous if a permission request is about to take place


        // TODO: we need to know PSM
        //public static void AddBleHostedL2Channel<TDelegate>(this IServiceCollection services, bool secure) where TDelegate : IL2CapEndpointDelegate
        //{
        //    // TODO: I could cheat and put some sort of context object in the services and pull it out to add services
        //}

        public static void AddBleHostedService<TService>(this IServiceCollection services, string serviceUuid, string characteristicUuid) where TService : BleGattService
        {
            // TODO: must still call IBleManager.StartAdvertising & IBleManager.AttachRegisteredServices()?
            // TODO: at least 1 method must be overridden
            // TODO: ensure BleGattServiceAttribute is good
            // TODO: if RemoveRegisteredServices was not called previous, we will auto hook if permissions are still available

            
        }
    }
}
