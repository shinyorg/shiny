using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class GyroscopeImpl : AbstractSensor<MotionReading>, IGyroscope
    {
        public GyroscopeImpl() : base(SensorType.Gyroscope) {}


        protected override MotionReading ToReading(SensorEvent e) => new MotionReading(e.Values[0], e.Values[1], e.Values[2]);
    }
}
