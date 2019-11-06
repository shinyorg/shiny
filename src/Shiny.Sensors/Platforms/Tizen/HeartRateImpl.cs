using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Tizen.Sensor;


namespace Shiny.Sensors
{
    public class HeartRateMonitorImpl : IHeartRateMonitor
    {
        public Task<AccessState> RequestAccess() => Platform.RequestAccess(Platform.Health);
        public bool IsAvailable => HeartRateMonitor.IsSupported;


        IObservable<ushort> observable;
        public IObservable<ushort> WhenReadingTaken()
        {
            this.observable = this.observable ?? Observable.Create<ushort>(ob =>
            {
                var handler = new EventHandler<HeartRateMonitorDataUpdatedEventArgs>((sender, args) =>
                    ob.OnNext((ushort)args.HeartRate)
                );
                var sensor = new HeartRateMonitor
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

            return this.observable;
        }
    }
}
