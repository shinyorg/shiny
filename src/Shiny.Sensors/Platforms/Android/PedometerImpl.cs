using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class PedometerImpl : AbstractSensor<int>, IPedometer
    {
        public PedometerImpl() : base(SensorType.StepCounter) {}
        protected override int ToReading(SensorEvent e) => Convert.ToInt32(e.Values[0]);
    }
}