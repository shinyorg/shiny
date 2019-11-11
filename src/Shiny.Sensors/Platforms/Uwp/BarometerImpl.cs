using System;
using System.Reactive.Linq;
using Windows.Devices.Sensors;
using Windows.Foundation;


namespace Shiny.Sensors
{
    public class BarometerImpl : IBarometer
    {
        readonly Barometer barometer = Barometer.GetDefault();
        public bool IsAvailable => this.barometer != null;


        IObservable<double>? readOb;
        public IObservable<double> WhenReadingTaken()
            => this.readOb ??= Observable.Create<double>(ob =>
            {
                var handler = new TypedEventHandler<Barometer, BarometerReadingChangedEventArgs>((sender, args) =>
                    ob.OnNext(args.Reading.StationPressureInHectopascals)
                );
                this.barometer.ReadingChanged += handler;
                return () => this.barometer.ReadingChanged -= handler;
            })
            .Publish()
            .RefCount();
    }
}
