using System;
using System.Reactive.Linq;
using CoreLocation;


namespace Shiny.Sensors
{
    public class CompassImpl : ICompass
    {
        public bool IsAvailable => CLLocationManager.HeadingAvailable;


        IObservable<CompassReading> readOb;
        public IObservable<CompassReading> WhenReadingTaken()
        {
            this.readOb = this.readOb ?? Observable.Create<CompassReading>(ob =>
            {
                var handler = new EventHandler<CLHeadingUpdatedEventArgs>((sender, args) =>
                {
                    var accuracy = this.FromNative(args.NewHeading.HeadingAccuracy);
                    var read = new CompassReading(accuracy, args.NewHeading.MagneticHeading, args.NewHeading.TrueHeading);
                    ob.OnNext(read);
                });
                var lm = new CLLocationManager
                {
                    DesiredAccuracy = CLLocation.AccuracyBest,
                    HeadingFilter = 1
                };
                lm.UpdatedHeading += handler;
                lm.StartUpdatingHeading();

                return () =>
                {
                    lm.StopUpdatingHeading();
                    lm.UpdatedHeading -= handler;
                };
            })
            .Publish()
            .RefCount();

            return this.readOb;
        }


        protected CompassAccuracy FromNative(double value) => value < 0 ? CompassAccuracy.Unreliable : CompassAccuracy.High;
    }
}
