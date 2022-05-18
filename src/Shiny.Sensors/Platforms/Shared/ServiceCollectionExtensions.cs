using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Sensors;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAllSensors(this IServiceCollection services)
        {
            services.AddAccelerometer();
            services.AddAmbientLightSensor();
            services.AddBarometer();
            services.AddCompass();
            services.AddMagnetometer();
            services.AddPedometer();
            services.AddProximitySensor();
            //services.AddHeartRateMonitor();
            services.AddTemperature();
            services.AddHumidity();
        }


        public static bool AddAccelerometer(this IServiceCollection services)
        {
#if !IOS && !MACCATALYST && !ANDROID
            return false;
#else
            services.TryAddSingleton<IAccelerometer, AccelerometerImpl>();
            return true;
#endif
        }


        public static bool AddAmbientLightSensor(this IServiceCollection services)
        {
#if !ANDROID
            return false;
#else
            services.TryAddSingleton<IAmbientLight, AmbientLightImpl>();
            return true;
#endif
        }


        public static bool AddBarometer(this IServiceCollection services)
        {
#if !IOS && !MACCATALYST && !ANDROID
            return false;
#else
            services.TryAddSingleton<IBarometer, BarometerImpl>();
            return true;
#endif
        }


        public static bool AddCompass(this IServiceCollection services)
        {
#if !IOS && !MACCATALYST && !ANDROID
            return false;
#else
            services.TryAddSingleton<ICompass, CompassImpl>();
            return true;
#endif
        }


        public static bool AddGyroscope(this IServiceCollection services)
        {
#if !IOS && !MACCATALYST && !ANDROID
            return false;
#else
            services.TryAddSingleton<IGyroscope, GyroscopeImpl>();
            return true;
#endif
        }


        public static bool AddHumidity(this IServiceCollection services)
        {
#if ANDROID
            services.TryAddSingleton<IHumidity, HumidityImpl>();
            return true;
#else
            return false;
#endif
        }


        public static bool AddMagnetometer(this IServiceCollection services)
        {
#if !IOS && !MACCATALYST && !ANDROID
            return false;
#else
            services.TryAddSingleton<IMagnetometer, MagnetometerImpl>();
            return true;
#endif
        }


        public static bool AddProximitySensor(this IServiceCollection services)
        {
#if !IOS && !MACCATALYST && !ANDROID
            return false;
#else
            services.TryAddSingleton<IProximity, ProximityImpl>();
            return true;
#endif
        }


        //        public static bool AddHeartRateMonitor(this IServiceCollection services)
        //        {
        //#if __ANDROID__ || __WATCHOS__ || TIZEN
        //            services.TryAddSingleton<IHeartRateMonitor, HeartRateMonitorImpl>();
        //            return true;
        //#else
        //            return false;
        //#endif
        //        }


        public static bool AddPedometer(this IServiceCollection services)
        {
#if !IOS && !MACCATALYST && !ANDROID
            return false;
#else
            services.TryAddSingleton<IPedometer, PedometerImpl>();
            return true;
#endif
        }


        public static bool AddTemperature(this IServiceCollection services)
        {
#if !ANDROID
            return false;
#else
            services.TryAddSingleton<ITemperature, TemperatureImpl>();
            return true;
#endif
        }
    }
}
//        public static CardinalDirection GetDirection(this CompassReading reading, bool AddTrueHeading = false)
//        {
//            var r = AddTrueHeading && reading.TrueHeading != null ? reading.TrueHeading.Value : reading.MagneticHeading;

//            return CardinalDirection.E;
//        }

////N   - 348.75 - 11.25
////NNE - 11.25 - 33.75
////NE  - 33.75 - 56.25
////ENE - 56.25 - 78.75
////E   - 78.75 - 101.25
////ESE - 101.25 - 123.75
////SE  - 123.75 - 146.25
////SSE - 146.25 - 168.75
////S   - 168.75 - 191.25
////SSW - 191.25 - 213.75
////SW  - 213.75 - 236.25
////WSW - 236.25 - 258.75
////W   - 258.75 - 281.25
////WNW - 281.25 - 303.75
////NW  - 303.75 - 326.25
////NNW - 326.25 - 348.75