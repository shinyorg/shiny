using System;
using System.Reactive.Linq;
using Tizen.Sensor;


namespace Shiny.Sensors
{
    public class GyroscopeImpl : IGyroscope
    {
        public bool IsAvailable => Gyroscope.IsSupported;


        IObservable<MotionReading>? observable;
        public IObservable<MotionReading> WhenReadingTaken()
            => this.observable ??= Observable.Create<MotionReading>(ob =>
            {
                var handler = new EventHandler<GyroscopeDataUpdatedEventArgs>((sender, args) =>
                    ob.OnNext(new MotionReading(args.X, args.Y, args.Z))
                );
                var sensor = new Gyroscope
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
    }
}
