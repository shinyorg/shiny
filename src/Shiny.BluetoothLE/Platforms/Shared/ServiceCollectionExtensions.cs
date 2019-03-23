using System;
using Shiny.BluetoothLE.Central;
using Shiny.BluetoothLE.Peripherals;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.BluetoothLE
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseBleCentral(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<ICentralManager, CentralManager>();
            return true;
#endif
        }


        public static bool UseBleCentral<T>(this IServiceCollection builder) where T : class, IBleStateRestoreDelegate
        {
            builder.AddSingleton<IBleStateRestoreDelegate, T>();
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<ICentralManager, CentralManager>();
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
