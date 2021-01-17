using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Shiny.Sensors
{
    public class SharedCompassImpl : ICompass
    {
        readonly IAccelerometer? accelerometer;
        readonly IMagnetometer? magnetometer;


        public SharedCompassImpl(IAccelerometer? accelerometer = null, IMagnetometer? magnetometer = null)
        {
            this.accelerometer = accelerometer;
            this.magnetometer = magnetometer;
        }


        public bool IsAvailable => (this.accelerometer?.IsAvailable ?? false) &&
                                   (this.magnetometer?.IsAvailable ?? false);

        public IObservable<CompassReading> WhenReadingTaken()
        {
            if (!this.IsAvailable)
                return Observable.Empty<CompassReading>();

            return Observable.Create<CompassReading>(ob =>
            {
                var comp = new CompositeDisposable();
                var syncLock = new object();
                MotionReading? lastAccel = null;
                MotionReading? lastMag = null;

                comp.Add(this.accelerometer
                    .WhenReadingTaken()
                    .Synchronize(syncLock)
                    .Subscribe(x =>
                    {
                        lastAccel = x;
                        Calc(ob, ref lastMag, ref lastAccel);
                    })
                );

                comp.Add(this.magnetometer
                    .WhenReadingTaken()
                    .Synchronize(syncLock)
                    .Subscribe(x =>
                    {
                        lastMag = x;
                        Calc(ob, ref lastMag, ref lastAccel);
                    })
                );

                return comp;
            });
        }

        static void Calc(IObserver<CompassReading> ob, ref MotionReading? lastMag, ref MotionReading? lastAccel)
        {
            if (lastMag == null || lastAccel == null)
                return;

            //    SensorManager.GetRotationMatrix(this.rMatrix, null, this.lastAccel, this.lastMag);
            //    SensorManager.GetOrientation(this.rMatrix, this.orientation);
            //    var degrees = (Math.ToDegrees(this.orientation[0]) + 360) % 360;

            // TODO: get compass accuracy
            // TODO: calculate true north
            //ob.OnNext(new CompassReading(CompassAccuracy.Approximate, degrees, null));

            // clear for fresh read
            lastMag = null;
            lastAccel = null;
        }
    }
}
