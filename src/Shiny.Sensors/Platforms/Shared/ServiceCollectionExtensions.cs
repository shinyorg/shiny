using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Sensors;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseAllSensors(this IServiceCollection services)
        {
            services.UseAccelerometer();
            services.UseAmbientLightSensor();
            services.UseBarometer();
            services.UseCompass();
            services.UseMagnetometer();
            services.UsePedometer();
            services.UseProximitySensor();
            services.UseHeartRateMonitor();
            services.UseTemperature();
            services.UseHumidity();
        }


        public static bool UseAccelerometer(this IServiceCollection services)
        {
#if NETSTANDARD
            return false;
#else
            services.TryAddSingleton<IAccelerometer, AccelerometerImpl>();
            return true;
#endif
        }


        public static bool UseAmbientLightSensor(this IServiceCollection services)
        {
#if NETSTANDARD || __IOS__ || __WATCHOS__
            return false;
#else
            services.TryAddSingleton<IAmbientLight, AmbientLightImpl>();
            return true;
#endif
        }


        public static bool UseBarometer(this IServiceCollection services)
        {
#if NETSTANDARD || TIZEN
            return false;
#else
            services.TryAddSingleton<IBarometer, BarometerImpl>();
            return true;
#endif
        }


        public static bool UseCompass(this IServiceCollection services)
        {
#if NETSTANDARD
            return false;
#elif TIZEN || __WATCHOS__
            services.UseAccelerometer();
            services.UseMagnetometer();
            services.TryAddSingleton<ICompass, SharedCompassImpl>();
            return true;
#else
            services.TryAddSingleton<ICompass, CompassImpl>();
            return true;
#endif
        }


        public static bool UseGyroscope(this IServiceCollection services)
        {
#if NETSTANDARD
            return false;
#else
            services.TryAddSingleton<IGyroscope, GyroscopeImpl>();
            return true;
#endif
        }


        public static bool UseHumidity(this IServiceCollection services)
        {
#if __ANDROID__ || TIZEN
            services.TryAddSingleton<IHumidity, HumidityImpl>();
            return true;
#else
            return false;
#endif
        }


        public static bool UseMagnetometer(this IServiceCollection services)
        {
#if NETSTANDARD
            return false;
#else
            services.TryAddSingleton<IMagnetometer, MagnetometerImpl>();
            return true;
#endif
        }


        public static bool UseProximitySensor(this IServiceCollection services)
        {
#if __IOS__ || __ANDROID__
            services.TryAddSingleton<IProximity, ProximityImpl>();
            return true;
#else
            return false;
#endif
        }


        public static bool UseHeartRateMonitor(this IServiceCollection services)
        {
#if __ANDROID__ || __WATCHOS__ || TIZEN
            services.TryAddSingleton<IHeartRateMonitor, HeartRateMonitorImpl>();
            return true;
#else
            return false;
#endif
        }


        public static bool UsePedometer(this IServiceCollection services)
        {
#if __ANDROID__ || __IOS__ || __WATCHOS__ || TIZEN
            services.TryAddSingleton<IPedometer, PedometerImpl>();
            return true;
#else
            return false;
#endif
        }


        public static bool UseTemperature(this IServiceCollection services)
        {
#if __ANDROID__ || TIZEN
            services.TryAddSingleton<ITemperature, TemperatureImpl>();
            return true;
#else
            return false;
#endif
        }
    }
}
//        public static CardinalDirection GetDirection(this CompassReading reading, bool useTrueHeading = false)
//        {
//            var r = useTrueHeading && reading.TrueHeading != null ? reading.TrueHeading.Value : reading.MagneticHeading;

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