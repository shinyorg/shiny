using System;
using System.Reactive.Linq;
using Windows.Devices.Sensors;
using Windows.Foundation;


namespace Shiny.Sensors
{
    public class CompassImpl : ICompass
    {
        readonly Compass compass;


        public CompassImpl()
        {
            this.compass = Compass.GetDefault();
        }


        public bool IsAvailable => this.compass != null;


        IObservable<CompassReading> readOb;
        public IObservable<CompassReading> WhenReadingTaken()
        {
            this.readOb = this.readOb ?? Observable.Create<CompassReading>(ob =>
            {
                var handler = new TypedEventHandler<Compass, CompassReadingChangedEventArgs>((sender, args) =>
                {
                    var accuracy = this.FromNative(args.Reading.HeadingAccuracy);
                    var read = new CompassReading(accuracy, args.Reading.HeadingMagneticNorth, args.Reading.HeadingTrueNorth ?? 0);
                    ob.OnNext(read);
                });
                this.compass.ReadingChanged += handler;

                return () => this.compass.ReadingChanged -= handler;
            })
            .Publish()
            .RefCount();

            return this.readOb;
        }


        protected CompassAccuracy FromNative(MagnetometerAccuracy native)
        {
            switch (native)
            {
                case MagnetometerAccuracy.High:
                    return CompassAccuracy.High;

                case MagnetometerAccuracy.Unreliable:
                    return CompassAccuracy.Unreliable;

   ;             case MagnetometerAccuracy.Approximate:
                    return CompassAccuracy.Approximate;

   ;             default:
                    return CompassAccuracy.Unknown;
   ;         }
        }
    }
}
