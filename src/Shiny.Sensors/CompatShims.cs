using System;


namespace Shiny.Sensors
{
    public static class CrossSensors
    {
        public static IAccelerometer Accelerometer => ShinyHost.Resolve<IAccelerometer>();
        public static IAmbientLight AmbientLight => ShinyHost.Resolve<IAmbientLight>();
    }
}
