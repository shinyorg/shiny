using System;
using System.Reactive.Linq;
using Tizen.Sensor;


namespace Shiny.Sensors
{
    public class AccelerometerImpl : IAccelerometer
    {
        public bool IsAvailable => Accelerometer.IsSupported;


        IObservable<MotionReading> observable;
        public IObservable<MotionReading> WhenReadingTaken()
        {
            this.observable = this.observable ?? Observable.Create<MotionReading>(ob =>
            {
                var handler = new EventHandler<AccelerometerDataUpdatedEventArgs>((sender, args) =>
                    ob.OnNext(new MotionReading(args.X, args.Y, args.Z))
                );
                var sensor = new Accelerometer
                {
                    Interval = 250
                };
                sensor.DataUpdated += handler;
                sensor.Start();

                return () =>
                {
                    sensor.Stop();
                    sensor.DataUpdated -= handler;
                    sensor.Dispose();
                };
            })
            .Publish()
            .RefCount();

            return this.observable;
        }
    }
}
