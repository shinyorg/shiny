using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class ProximityImpl : AbstractSensor<bool>, IProximity
    {
        public ProximityImpl() : base(SensorType.Proximity) {}
        protected override bool ToReading(SensorEvent e) => e.Values[0] < e.Sensor.MaximumRange;
    }
}
