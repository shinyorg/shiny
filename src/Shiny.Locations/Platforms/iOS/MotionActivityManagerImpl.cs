using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreMotion;
using Foundation;


namespace Shiny.Locations
{
    public class MotionActivityManagerImpl : IMotionActivityManager
    {
        readonly CMMotionActivityManager activityManager = new CMMotionActivityManager();


        public async Task<AccessState> RequestAccess()
        {
            if (!CMMotionActivityManager.IsActivityAvailable)
                return AccessState.NotSupported;

            if (CMMotionActivityManager.AuthorizationStatus == CMAuthorizationStatus.Authorized)
                return AccessState.Available;

            try
            {
                await this.Query(DateTimeOffset.UtcNow);
                await Task.Delay(500);
            }
            catch {}

            switch (CMMotionActivityManager.AuthorizationStatus)
            {
                case CMAuthorizationStatus.Denied:
                    return AccessState.Denied;

                case CMAuthorizationStatus.Restricted:
                    return AccessState.Restricted;

                case CMAuthorizationStatus.Authorized:
                default:
                    return AccessState.Available;
            }
        }


        public async Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null)
        {
            (await this.RequestAccess().ConfigureAwait(false)).Assert();

            end = end ?? DateTimeOffset.UtcNow;
            var results = await this.activityManager
                .QueryActivityAsync(
                    (NSDate)start.LocalDateTime,
                    (NSDate)end.Value.LocalDateTime,
                    NSOperationQueue.MainQueue
                )
                .ConfigureAwait(false);

            return results
                .Select(x => ToEvent(x))
                .ToList();
        }


        public IObservable<MotionActivityEvent> WhenActivityChanged() => Observable.Create<MotionActivityEvent>(async ob =>
        {
            var started = false;
            var access = await this.RequestAccess().ConfigureAwait(false);
            if (access != AccessState.Available)
            {
                ob.OnError(new PermissionException("MotionActivity", access));
            }
            else
            {
                this.activityManager.StartActivityUpdates(
                    NSOperationQueue.CurrentQueue,
                    target =>
                    {
                        var e = ToEvent(target);
                        ob.OnNext(e);
                    }
                );
                started = true;
            }
            return () =>
            {
                if (started)
                    this.activityManager.StopActivityUpdates();
            };
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
