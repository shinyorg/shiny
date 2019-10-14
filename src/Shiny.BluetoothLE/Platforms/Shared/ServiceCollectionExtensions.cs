using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE.Central;
using Shiny.BluetoothLE.Peripherals;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register the ICentralManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="delegateType"></param>
        /// <returns></returns>
        public static bool UseBleCentral(this IServiceCollection services, Type delegateType, BleCentralConfiguration config = null)
        {
            if (services.UseBleCentral(config))
            {
                if (delegateType != null)
                    services.AddSingleton(typeof(IBleCentralDelegate), delegateType);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Register the ICentralManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
        /// </summary>
        /// <typeparam name="TCentralDelegate"></typeparam>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool UseBleCentral<TCentralDelegate>(this IServiceCollection services, BleCentralConfiguration config = null) where TCentralDelegate : class, IBleCentralDelegate
            => services.UseBleCentral(typeof(TCentralDelegate), config);


        /// <summary>
        /// Register the ICentralManager service that allows you to connect to other BLE devices
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool UseBleCentral(this IServiceCollection builder, BleCentralConfiguration config = null)
        {
#if NETSTANDARD
            return false;
#else
            builder.RegisterModule(new BleCentralShinyModule(config ?? new BleCentralConfiguration()));
            return true;
#endif
        }


        /// <summary>
        /// Registers the IPeripheralManager service that allows you to be a host BLE device
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static bool UseBlePeripherals(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<IPeripheralManager, PeripheralManager>();
            return true;
#endif
        }
    }
}
