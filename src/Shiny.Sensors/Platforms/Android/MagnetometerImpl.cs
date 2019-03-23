using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class MagnetometerImpl : AbstractSensor<MotionReading>, IMagnetometer
    {
        public MagnetometerImpl() : base(SensorType.MagneticField) {}
        protected override MotionReading ToReading(SensorEvent e) => new MotionReading(e.Values[0], e.Values[1], e.Values[2]);
    }
}
