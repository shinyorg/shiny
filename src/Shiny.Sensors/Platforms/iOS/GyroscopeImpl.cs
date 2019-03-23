using System;
using CoreMotion;
using Foundation;


namespace Shiny.Sensors
{
    public class GyroscopeImpl : AbstractMotionSensor, IGyroscope
    {
        protected override bool IsSensorAvailable(CMMotionManager mgr) => mgr.AccelerometerAvailable;
        protected override void Start(CMMotionManager mgr, IObserver<MotionReading> ob) =>
            mgr.StartGyroUpdates(NSOperationQueue.CurrentQueue ?? new NSOperationQueue(), (data, err) =>
                ob.OnNext(new MotionReading(data.RotationRate.x, data.RotationRate.y, data.RotationRate.z))
            );


        protected override void Stop(CMMotionManager mgr) => mgr.StopGyroUpdates();
        protected override void SetReportInterval(CMMotionManager mgr, TimeSpan timeSpan) => mgr.GyroUpdateInterval = timeSpan.TotalSeconds;
    }
}
