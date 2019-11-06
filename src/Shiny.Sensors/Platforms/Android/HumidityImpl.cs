using System;
using Android.Hardware;

namespace Shiny.Sensors
{
    public class HumidityImpl : AbstractSensor<double>, IHumidity
    {
        public HumidityImpl() : base(SensorType.RelativeHumidity) {}
        protected override double ToReading(SensorEvent e) => e.Values[0];
    }
}
