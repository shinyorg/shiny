using System;
using System.Reactive.Linq;
using Windows.Devices.Sensors;
using Windows.Foundation;


namespace Shiny.Sensors
{
    public class AmbientLightImpl : IAmbientLight
    {
        readonly LightSensor sensor;


        public AmbientLightImpl()
        {
            this.sensor = LightSensor.GetDefault();
        }


        public bool IsAvailable => this.sensor != null;


        IObservable<double> readOb;
        public IObservable<double> WhenReadingTaken()
        {
            this.readOb = this.readOb ?? Observable.Create<double>(ob =>
            {
                var handler = new TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>((sender, args) =>
                    ob.OnNext(args.Reading.IlluminanceInLux)
                );
                this.sensor.ReadingChanged += handler;
                return () => this.sensor.ReadingChanged -= handler;
            })
            .Publish()
            .RefCount();

            return this.readOb;
        }
    }
}
