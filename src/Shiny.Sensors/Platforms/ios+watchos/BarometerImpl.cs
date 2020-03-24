using System;
using System.Reactive.Linq;
using CoreMotion;
using Foundation;


namespace Shiny.Sensors
{
    public class BarometerImpl : IBarometer
    {

        public bool IsAvailable => CMAltimeter.IsRelativeAltitudeAvailable;


        IObservable<double>? readOb;
        public IObservable<double> WhenReadingTaken()
            => this.readOb ??= Observable.Create<double>(ob =>
            {
                var altimeter = new CMAltimeter();
                altimeter.StartRelativeAltitudeUpdates(NSOperationQueue.CurrentQueue, (data, error) =>
                    ob.OnNext(data.Pressure.DoubleValue) // kilopascals
                );
                return () =>
                {
                    altimeter.StopRelativeAltitudeUpdates();
                    altimeter.Dispose();
                };
            })
            .Publish()
            .RefCount();
    }
}
