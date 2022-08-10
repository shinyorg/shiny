#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Sensors;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAllSensors(this IServiceCollection services) => services
        .AddAccelerometer()
        .AddBarometer()
        .AddCompass()
        .AddPedometer()
        .AddProximitySensor()
#if ANDROID
        .AddAmbientLightSensor()
        .AddHumidity()
        .AddTemperature()
#endif
        .AddMagnetometer();


#if ANDROID
    public static IServiceCollection AddAmbientLightSensor(this IServiceCollection services)
    {
        services.TryAddSingleton<IAmbientLight, AmbientLightImpl>();
        return services;
    }

    public static IServiceCollection AddTemperature(this IServiceCollection services)
    {
        services.TryAddSingleton<ITemperature, TemperatureImpl>();
        return services;
    }


    public static IServiceCollection AddHumidity(this IServiceCollection services)
    {
        services.TryAddSingleton<IHumidity, HumidityImpl>();
        return services;
    }

#endif

    public static IServiceCollection AddAccelerometer(this IServiceCollection services)
    {
        services.TryAddSingleton<IAccelerometer, AccelerometerImpl>();
        return services;
    }


    public static IServiceCollection AddBarometer(this IServiceCollection services)
    {
        services.TryAddSingleton<IBarometer, BarometerImpl>();
        return services;
    }


    public static IServiceCollection AddCompass(this IServiceCollection services)
    {
        services.TryAddSingleton<ICompass, CompassImpl>();
        return services;
    }


    public static IServiceCollection AddGyroscope(this IServiceCollection services)
    {
        services.TryAddSingleton<IGyroscope, GyroscopeImpl>();
        return services;
    }


    public static IServiceCollection AddMagnetometer(this IServiceCollection services)
    {
        services.TryAddSingleton<IMagnetometer, MagnetometerImpl>();
        return services;
    }


    public static IServiceCollection AddProximitySensor(this IServiceCollection services)
    {
        services.TryAddSingleton<IProximity, ProximityImpl>();
        return services;
    }


    public static IServiceCollection AddPedometer(this IServiceCollection services)
    {
        services.TryAddSingleton<IPedometer, PedometerImpl>();
        return services;
    }
}
#endif
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