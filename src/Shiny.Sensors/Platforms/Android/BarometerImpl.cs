using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class BarometerImpl : AbstractSensor<double>, IBarometer
    {
        public BarometerImpl() : base(SensorType.Pressure) {}


        //1 meter sea water [msw] = 100 hectopascal [hPa]
        protected override double ToReading(SensorEvent e) => e.Values[0]; // hpa
    }
}