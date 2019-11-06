using System;
using System.Reactive.Linq;
using Tizen.Sensor;


namespace Shiny.Sensors
{
    public class HumidityImpl : IHumidity
    {
        public bool IsAvailable => HumiditySensor.IsSupported;


        IObservable<double> observable;
        public IObservable<double> WhenReadingTaken()
        {
            this.observable = this.observable ?? Observable.Create<double>(ob =>
            {
                var handler = new EventHandler<HumiditySensorDataUpdatedEventArgs>((sender, args) =>
                    ob.OnNext(args.Humidity)
                );
                var sensor = new HumiditySensor
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
