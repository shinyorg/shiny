using System;
using System.Reactive.Linq;
using Windows.Devices.Sensors;
using Windows.Foundation;


namespace Shiny.Sensors
{
    public class GyroscopeImpl : IGyroscope
    {
        readonly Gyrometer gyrometer = Gyrometer.GetDefault();

        public bool IsAvailable => this.gyrometer != null;


        IObservable<MotionReading>? readOb;
        public IObservable<MotionReading> WhenReadingTaken()
            => this.readOb ??= Observable.Create<MotionReading>(ob =>
            {
                var handler = new TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs>((sender, args) =>
                    ob.OnNext(new MotionReading(
                        args.Reading.AngularVelocityX,
                        args.Reading.AngularVelocityY,
                        args.Reading.AngularVelocityZ
                    ))
                );
                this.gyrometer.ReadingChanged += handler;

                return () => this.gyrometer.ReadingChanged -= handler;
            });
    }
}
