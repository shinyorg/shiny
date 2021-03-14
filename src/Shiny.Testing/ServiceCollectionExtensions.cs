using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE;
using Shiny.Locations;
using Shiny.Power;
using Shiny.Push;
using Shiny.Stores;
using Shiny.Testing.BluetoothLE;
using Shiny.Testing.Locations;
using Shiny.Testing.Power;
using Shiny.Testing.Push;
using Shiny.Testing.Stores;


namespace Shiny.Testing
{
    public static class ServiceCollectionExtensions
    {
        public static void UseTestStores(this IServiceCollection services)
            => services.AddSingleton<IKeyValueStore, TestKeyValueStore>();

        public static void UseTestPower(this IServiceCollection services)
            => services.AddSingleton<IPowerManager, TestPowerManager>();

        public static void UseTestBleClient(this IServiceCollection services)
            => services.AddSingleton<IBleManager, TestBleManager>();

        public static void UseTestGeofencing(this IServiceCollection services)
            => services.AddSingleton<IGeofenceManager, TestGeofenceManager>();

        public static void UseTestGps(this IServiceCollection services)
            => services.AddSingleton<IGpsManager, TestGpsManager>();

        public static void UseTestMotionActivity(this IServiceCollection services, MotionActivityType? generateTestData = null)
        {
            var mgr = new TestMotionActivityManager();
            if (generateTestData != null)
            {
                mgr.StartGeneratingTestData(
                    generateTestData.Value,
                    TimeSpan.FromSeconds(10)
                );
            }
            services.AddSingleton<IMotionActivityManager>(mgr);
        }


        public static void UseTestPush(this IServiceCollection services)
            => services.AddSingleton<IPushManager, TestPushManager>();
    }
}
