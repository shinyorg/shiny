using System;
using System.Reactive.Linq;
using Tizen.Sensor;

namespace Shiny.Sensors
{
    public class PedometerImpl : IPedometer
    {
        public bool IsAvailable => Pedometer.IsSupported;


        IObservable<int>? observable;
        public IObservable<int> WhenReadingTaken()
            => this.observable ??= Observable.Create<int>(ob =>
            {
                var handler = new EventHandler<PedometerDataUpdatedEventArgs>((sender, args) =>
                    ob.OnNext((int)args.StepCount)
                );
                var sensor = new Pedometer
                {
                    Interval = 1000
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
