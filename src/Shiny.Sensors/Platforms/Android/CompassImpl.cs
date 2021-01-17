using System;
using System.Linq;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Hardware;
using Math = Java.Lang.Math;


namespace Shiny.Sensors
{
    public class CompassImpl : ICompass
    {
        readonly object syncLock;
        readonly SensorManager sensorManager;
        readonly IObservable<CompassReading> readOb;
        readonly float[] rMatrix = new float[9];
        readonly float[] orientation = new float[3];
        float[]? lastAccel;
        float[]? lastMag;


        public CompassImpl()
        {
            this.syncLock = new object();
            this.sensorManager = (SensorManager)Application.Context.GetSystemService(Context.SensorService);
            this.readOb = Observable.Create<CompassReading>(ob =>
            {
                var accelMgr = new ShinySensorManager(this.sensorManager);
                var magMgr = new ShinySensorManager(this.sensorManager);
                accelMgr.Start(SensorType.Accelerometer, SensorDelay.Fastest, e =>
                {
                    lock (this.syncLock)
                    {
                        this.lastAccel = e.Values.ToArray();
                        this.Calc(ob);
                    }
                });
                magMgr.Start(SensorType.MagneticField, SensorDelay.Fastest, e =>
                {
                    lock (this.syncLock)
                    {
                        this.lastMag = e.Values.ToArray();
                        this.Calc(ob);
                    }
                });
                return () =>
                {
                    accelMgr.Stop();
                    magMgr.Stop();
                };
            })
            .Publish()
            .RefCount();
        }


        public bool IsAvailable => this.sensorManager.GetDefaultSensor(SensorType.Accelerometer) != null &&
                                   this.sensorManager.GetDefaultSensor(SensorType.MagneticField) != null;
        public IObservable<CompassReading> WhenReadingTaken() => this.IsAvailable ? this.readOb : Observable.Empty<CompassReading>();


        void Calc(IObserver<CompassReading> ob)
        {
            if (this.lastMag == null || this.lastAccel == null)
                return;

            SensorManager.GetRotationMatrix(this.rMatrix, null, this.lastAccel, this.lastMag);
            SensorManager.GetOrientation(this.rMatrix, this.orientation);
            var degrees = (Math.ToDegrees(this.orientation[0]) + 360) % 360;

            // TODO: get compass accuracy
            // TODO: calculate true north
            ob.OnNext(new CompassReading(CompassAccuracy.Approximate, degrees, null));

            // clear for fresh read
            this.lastMag = null;
            this.lastAccel = null;
        }
    }
}