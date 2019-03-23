using System;
using System.Reactive.Linq;
using Windows.Devices.Sensors;
using Windows.Foundation;


namespace Shiny.Sensors
{
    public class DeviceOrientationImpl : IDeviceOrientation
    {
        readonly SimpleOrientationSensor sensor;


        public DeviceOrientationImpl()
        {
            this.sensor = SimpleOrientationSensor.GetDefault();
        }


        public bool IsAvailable => this.sensor != null;


        IObservable<DeviceOrientation> readOb;
        public IObservable<DeviceOrientation> WhenReadingTaken()
        {
            this.readOb = this.readOb ?? Observable.Create<DeviceOrientation>(ob =>
            {
                this.Broadcast(ob, this.sensor.GetCurrentOrientation()); // startwith
                var handler = new TypedEventHandler<SimpleOrientationSensor, SimpleOrientationSensorOrientationChangedEventArgs>(
                    (sender, args) => this.Broadcast(ob, args.Orientation));

                this.sensor.OrientationChanged += handler;
                return () => this.sensor.OrientationChanged -= handler;
            })
            .Publish()
            .RefCount()
            .Repeat(1);

            return this.readOb;
        }


        void Broadcast(IObserver<DeviceOrientation> ob, SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.NotRotated:
                    ob.OnNext(DeviceOrientation.LandscapeLeft);
                    break;

                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    ob.OnNext(DeviceOrientation.Portrait);
                    break;

                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    ob.OnNext(DeviceOrientation.LandscapeRight);
                    break;

                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    ob.OnNext(DeviceOrientation.PortraitUpsideDown);
                    break;
            }
        }
    }
}