using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Central;
using Shiny.BluetoothLE.Peripherals;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterBleStateRestore<T>(this IServiceCollection services)
            where T : class, IBleStateRestoreDelegate
            => services.AddSingleton<IBleStateRestoreDelegate, T>();


        public static void RegisterBleAdapterState<T>(this IServiceCollection services)
            where T : class, IBleAdapterDelegate
            => services.AddSingleton<IBleAdapterDelegate, T>();


        public static bool UseBleCentral(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<ICentralManager, CentralManager>();
            return true;
#endif
        }



#if __IOS__
        public static bool UseBleCentral(this IServiceCollection builder, BleAdapterConfiguration config = null)
        {
            builder.AddSingleton<ICentralManager, CentralManager>();
            return true;
        }

#endif


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
