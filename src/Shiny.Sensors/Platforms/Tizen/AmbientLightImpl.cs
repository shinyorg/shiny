using System;
using System.Reactive.Linq;
using Tizen.Sensor;


namespace Shiny.Sensors
{
    public class AmbientLightImpl : IAmbientLight
    {
        public bool IsAvailable => LightSensor.IsSupported;


        IObservable<double>? observable;
        public IObservable<double> WhenReadingTaken()
            => this.observable ??= Observable.Create<double>(ob =>
            {
                var handler = new EventHandler<LightSensorDataUpdatedEventArgs>((sender, args) =>
                    ob.OnNext(args.Level)
                );
                var sensor = new LightSensor
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
