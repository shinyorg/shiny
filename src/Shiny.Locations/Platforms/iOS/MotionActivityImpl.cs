using System;
using System.Reactive.Linq;
using CoreMotion;
using Foundation;


namespace Shiny.Locations
{
    public class MotionActivityImpl : IMotionActivity
    {
        readonly CMMotionActivityManager activityManager;


        public MotionActivityImpl()
        {
            this.activityManager = new CMMotionActivityManager();
            //this.activityManager.QueryActivityAsync
        }


        //public IObservable<MotionActivityEvent> WhenActivityChanged()
        //    => Observable.Create<MotionActivityEvent>(ob =>
        //    {
        //        //CMMotionActivityManager.IsActivityAvailable
        //        //CMMotionActivityManager.AuthorizationStatus == CMAuthorizationStatus.Authorized
        //        var am = new CMMotionActivityManager();
        //        am.StartActivityUpdates(NSOperationQueue.CurrentQueue, target =>
        //        {
        //            var flags = MotionFlags.Unknown;
        //            if (!target.Unknown)
        //            {
        //                flags &= MotionFlags.Unknown;
        //                if (target.Automotive)
        //                    flags |= MotionFlags.Automotive;

        //                if (target.Cycling)
        //                    flags |= MotionFlags.Cycling;

        //                if (target.Running)
        //                    flags |= MotionFlags.Running;

        //                if (target.Stationary)
        //                    flags |= MotionFlags.Stationary;

        //                if (target.Walking)
        //                    flags |= MotionFlags.Walking;
        //            }

        //            var conf = (Confidence)Enum.Parse(typeof(Confidence), target.Confidence.ToString(), true);
        //            ob.OnNext(new MotionActivityEvent(flags, conf, (DateTime)target.StartDate));
        //        });
        //        return () =>
        //        {
        //            am.StopActivityUpdates();
        //            am.Dispose();
        //        };
        //    })
        //    .Repeat(1);
    }
}
