using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE.Central;
using Shiny.BluetoothLE.Peripherals;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        //public static void RegisterBlePeripheralDelegate<T>(this IServiceCollection services)
        //    where T : class, IBlePeripheralDelegate
        //    => services.AddSingleton<IBlePeripheralDelegate, T>();


        //public static void RegisterBleAdapterState<T>(this IServiceCollection services)
        //    where T : class, IBleAdapterDelegate
        //    => services.AddSingleton<IBleAdapterDelegate, T>();


        public static bool UseBleCentral(this IServiceCollection builder, BleCentralConfiguration config = null)
        {
#if NETSTANDARD
            return false;
#else
            builder.RegisterModule(new BleCentralShinyModule(config));
            return true;
#endif
        }


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
