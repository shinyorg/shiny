using System;
using System.Reactive.Linq;
using Windows.Devices.Sensors;
using Windows.Foundation;


namespace Shiny.Sensors
{
    public class MagnetometerImpl : IMagnetometer
    {
        readonly Magnetometer magnetometer = Magnetometer.GetDefault();


        public TimeSpan ReportInterval { get; set; }
        public bool IsAvailable => this.magnetometer != null;


        IObservable<MotionReading>? readOb;
        public IObservable<MotionReading> WhenReadingTaken()
            => this.readOb ??= Observable.Create<MotionReading>(ob =>
            {
                //this.magnetometer.ReportInterval;
                var handler = new TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs>((sender, args) =>
                    ob.OnNext(new MotionReading(
                        args.Reading.MagneticFieldX,
                        args.Reading.MagneticFieldY,
                        args.Reading.MagneticFieldZ
                    ))
                );
                this.magnetometer.ReadingChanged += handler;
                return () => this.magnetometer.ReadingChanged -= handler;
            });
    }
}
