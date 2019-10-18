using System;
using Shiny.Sensors;

namespace Shiny
{
    public static class CrossSensors
    {
        public static IAccelerometer Accelerometer => ShinyHost.Resolve<IAccelerometer>();
        public static IAmbientLight AmbientLight => ShinyHost.Resolve<IAmbientLight>();
        public static IBarometer Barometer => ShinyHost.Resolve<IBarometer>();
        public static ICompass Compass => ShinyHost.Resolve<ICompass>();
        public static IHeartRateMonitor HeartRate => ShinyHost.Resolve<IHeartRateMonitor>();
        public static IHumidity Humidity => ShinyHost.Resolve<IHumidity>();
        public static IMagnetometer Magnetometer => ShinyHost.Resolve<IMagnetometer>();
        public static IPedometer Pedometer => ShinyHost.Resolve<IPedometer>();
        public static IProximity Proximity => ShinyHost.Resolve<IProximity>();
        public static ITemperature Temperature => ShinyHost.Resolve<ITemperature>();
    }
}
