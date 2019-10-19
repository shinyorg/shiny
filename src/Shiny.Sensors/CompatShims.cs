using System;
using Shiny.Sensors;

namespace Shiny
{
    public static class CrossSensors
    {
        public static IAccelerometer Accelerometer { get; } = ShinyHost.Resolve<IAccelerometer>();
        public static IAmbientLight AmbientLight { get; } = ShinyHost.Resolve<IAmbientLight>();
        public static IBarometer Barometer { get; } = ShinyHost.Resolve<IBarometer>();
        public static ICompass Compass { get; } = ShinyHost.Resolve<ICompass>();
        public static IHeartRateMonitor HeartRate { get; } = ShinyHost.Resolve<IHeartRateMonitor>();
        public static IHumidity Humidity { get; } = ShinyHost.Resolve<IHumidity>();
        public static IMagnetometer Magnetometer { get; } = ShinyHost.Resolve<IMagnetometer>();
        public static IPedometer Pedometer { get; } = ShinyHost.Resolve<IPedometer>();
        public static IProximity Proximity { get; } = ShinyHost.Resolve<IProximity>();
        public static ITemperature Temperature { get; } = ShinyHost.Resolve<ITemperature>();
    }
}
