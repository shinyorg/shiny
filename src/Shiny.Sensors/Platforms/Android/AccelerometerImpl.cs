using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class AccelerometerImpl : AbstractSensor<MotionReading>, IAccelerometer
    {
        public AccelerometerImpl() : base(SensorType.Accelerometer) {}
        protected override MotionReading ToReading(SensorEvent e) => new MotionReading(e.Values[0], e.Values[1], e.Values[2]);
    }
}
