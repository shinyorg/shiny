using System;
using System.Reactive.Linq;
using Tizen.Sensor;


namespace Shiny.Sensors
{
    public class TemperatureImpl : ITemperature
    {
        public bool IsAvailable => TemperatureSensor.IsSupported;


        IObservable<double>? observable;
        public IObservable<double> WhenReadingTaken()
            => this.observable ??= Observable.Create<double>(ob =>
            {
                var handler = new EventHandler<TemperatureSensorDataUpdatedEventArgs>((sender, args) =>
                    ob.OnNext(args.Temperature)
                );
                var sensor = new TemperatureSensor
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
