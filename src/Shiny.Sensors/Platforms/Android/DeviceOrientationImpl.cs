using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class DeviceOrientationImpl : AbstractSensor<DeviceOrientation>, IDeviceOrientation
    {
        public DeviceOrientationImpl() : base(SensorType.Orientation) {}


        protected override DeviceOrientation ToReading(SensorEvent e)
        {
            var degrees = e.Values[0];
            if (degrees >= 0 && degrees < 90)
                return DeviceOrientation.Portrait;

            if (degrees >= 90 && degrees < 180)
                return DeviceOrientation.LandscapeLeft;

            if (degrees >= 180 && degrees < 270)
                return DeviceOrientation.PortraitUpsideDown;

            if (degrees >= 270 && degrees < 360)
                return DeviceOrientation.LandscapeRight;

            throw new ArgumentException("Invalid rotational degrees - " + degrees);
        }
    }
}