using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class TemperatureImpl : AbstractSensor<double>, ITemperature
    {
        public TemperatureImpl() : base(SensorType.Temperature) {}
        protected override double ToReading(SensorEvent e) => e.Values[0];
    }
}
