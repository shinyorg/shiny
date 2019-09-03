using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
        }


        public bool IsSupported => CMMotionActivityManager.IsActivityAvailable &&
                                   CMMotionActivityManager.AuthorizationStatus != CMAuthorizationStatus.Denied;


        public async Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset end)
        {
            var results = await this.activityManager.QueryActivityAsync(
                (NSDate)start.LocalDateTime,
                (NSDate)end.LocalDateTime,
                NSOperationQueue.MainQueue
            );
            return results
                .Select(x => ToEvent(x))
                .ToList();
        }


        public IObservable<MotionActivityEvent> WhenActivityChanged() => Observable.Create<MotionActivityEvent>(ob =>
        {
            this.activityManager.StartActivityUpdates(
                NSOperationQueue.CurrentQueue,
                target =>
                {
                    var e = ToEvent(target);
                    ob.OnNext(e);
                }
            );
            return () => this.activityManager.StopActivityUpdates();
        });


        static MotionActivityEvent ToEvent(CMMotionActivity target)
        {
            var flags = MotionActivityType.Unknown;
            if (!target.Unknown)
            {
                flags &= MotionActivityType.Unknown;
                if (target.Automotive)
                    flags |= MotionActivityType.Automotive;

                if (target.Cycling)
                    flags |= MotionActivityType.Cycling;

                if (target.Running)
                    flags |= MotionActivityType.Running;

                if (target.Stationary)
                    flags |= MotionActivityType.Stationary;

                if (target.Walking)
                    flags |= MotionActivityType.Walking;
            }

            var conf = (MotionActivityConfidence)Enum.Parse(typeof(MotionActivityConfidence), target.Confidence.ToString(), true);
            var activity = new MotionActivityEvent(flags, conf, (DateTime)target.StartDate);
            return activity;
        }
    }
}
