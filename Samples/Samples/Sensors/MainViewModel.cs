using System;
using System.Collections.Generic;
using Shiny.Sensors;


namespace Samples.Sensors
{
    public class MainViewModel
    {
        public List<ISensorViewModel> Sensors { get; }


        public MainViewModel(IAccelerometer accelerometer = null,
                             IGyroscope gyroscope = null,
                             IMagnetometer magnetometer = null,
                             ICompass compass = null,
                             IDeviceOrientation orientation = null,
                             IAmbientLight ambientLight = null,
                             IBarometer barometer = null,
                             IPedometer pedometer = null,
                             IProximity proximity = null)
        {

            this.Sensors = new List<ISensorViewModel>();
            this.AddIf(accelerometer, "G");
            this.AddIf(gyroscope, "G");
            this.AddIf(magnetometer, "M");
            this.AddIf(compass, "D");
            this.AddIf(orientation, "Position");
            this.AddIf(ambientLight, "Light");
            this.AddIf(barometer, "Pressure");
            this.AddIf(pedometer, "Steps");
            this.AddIf(proximity, "Near");
        }


        void AddIf<T>(ISensor<T> sensor, string measurement)
        {
            if (sensor != null)
                this.Sensors.Add(new SensorViewModel<T>(sensor, measurement));
        }
    }
}
