using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class AmbientLightImpl : AbstractSensor<double>, IAmbientLight
    {
        public AmbientLightImpl() : base(SensorType.Light) {}
        protected override double ToReading(SensorEvent e) => e.Values[0];
    }
}