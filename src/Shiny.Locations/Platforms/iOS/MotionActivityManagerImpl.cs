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

            switch (CMMotionActivityManager.AuthorizationStatus)
            {
                case CMAuthorizationStatus.Denied     : return AccessState.Denied;
                case CMAuthorizationStatus.Restricted : return AccessState.Restricted;
                case CMAuthorizationStatus.Authorized : return AccessState.Available;

                case CMAuthorizationStatus.NotDetermined:
                default:
                    var tcs = new TaskCompletionSource<AccessState>();

                    this.activityManager.QueryActivity(
                        NSDate.Now,
                        NSDate.Now,
                        NSOperationQueue.MainQueue,
                        (_, e) =>
                        {
                            if (e == null)
                            {
                                tcs.SetResult(AccessState.Available);
                            }
                            else
                            {
                                switch (e.Code)
                                {
                                    case (int)CMError.MotionActivityNotAuthorized:
                                        tcs.SetResult(AccessState.Denied);
                                        break;

                                    case (int)CMError.MotionActivityNotAvailable:
                                        tcs.SetResult(AccessState.NotSupported);
                                        break;

                                    case (int)CMError.MotionActivityNotEntitled:
                                        tcs.SetResult(AccessState.NotSetup);
                                        break;
                                }
                            }
                        }
                    );
                    return await tcs.Task.ConfigureAwait(false);
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


        IObservable<MotionActivityEvent>? activityObs;
        public IObservable<MotionActivityEvent> WhenActivityChanged()
        {
            this.activityObs ??= Observable
                .Create<MotionActivityEvent>(ob =>
                {
                    this.RequestAccess().ContinueWith(result =>
                    {
                        if (result.Result != AccessState.Available)
                        {
                            ob.OnError(new PermissionException("MotionActivity", result.Result));
                        }
                        else
                        {
                            this.activityManager.StartActivityUpdates(
                                NSOperationQueue.CurrentQueue,
                                e => ob.OnNext(ToEvent(e))
                            );
                        }
                    });
                    return () => this.activityManager.StopActivityUpdates();
                })
                .Publish()
                .RefCount();

            return this.activityObs;
        }


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
