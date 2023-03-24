using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreMotion;
using Foundation;

namespace Shiny.Locations;


public class MotionActivityManagerImpl : IMotionActivityManager
{
    readonly CMMotionActivityManager activityManager = new();


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
                        this.activityManager.StopActivityUpdates();
                        this.activityManager.StartActivityUpdates(
                            NSOperationQueue.MainQueue,
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


    protected static MotionActivityEvent ToEvent(CMMotionActivity target)
    {
        var conf = (MotionActivityConfidence)Enum.Parse(typeof(MotionActivityConfidence), target.Confidence.ToString(), true);
        var list = new List<DetectedMotionActivity>();

        if (!target.Unknown)
        {
            if (target.Automotive)
                list.Add(new(MotionActivityType.Automotive, conf));

            if (target.Cycling)
                list.Add(new(MotionActivityType.Cycling, conf));

            if (target.Running)
                list.Add(new(MotionActivityType.Running, conf));

            if (target.Walking)
                list.Add(new(MotionActivityType.Automotive, conf));

            if (target.Stationary)
                list.Add(new(MotionActivityType.Stationary, conf));
        }

        var activity = new MotionActivityEvent(
            list,
            list.FirstOrDefault(),
            (DateTime)target.StartDate
        );
        return activity;
    }
}
