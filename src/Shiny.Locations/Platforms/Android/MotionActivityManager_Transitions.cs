using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Android.Gms.Location;

namespace Shiny.Locations;


public partial class MotionActivityManager : IAndroidMotionActivityManager
{
    readonly Subject<MotionActivityTransition> transitionSubj = new();
    public IObservable<MotionActivityTransition> WhenTransition() => this.transitionSubj;


    public async Task StartTransitionWatch(params MotionActivityType[] typesToWatch)
    {
        if (typesToWatch.Length == 0)
            throw new ArgumentException("You must watch at least 1 motion activity type");

        (await this.RequestAccessInternal(false)).Assert();

        var list = new List<ActivityTransition>();
        foreach (var type in typesToWatch)
        {
            list.Add(CreateTransition(type, true));
            list.Add(CreateTransition(type, false));
        }
        var request = new ActivityTransitionRequest(list);
        var intent = this.GetPendingIntent();

        await this.client.RequestActivityTransitionUpdates(request, intent).ToTask();
    }


    public async Task StopTransitionWatch()
    {
        var intent = this.GetPendingIntent();
        await this.client.RemoveActivityTransitionUpdates(intent).ToTask();
    }


    protected async Task ProcessTransition(ActivityTransitionResult result)
    {
        foreach (var e in result.TransitionEvents)
        {
            var entry = e.TransitionType == ActivityTransition.ActivityTransitionEnter;

            MotionActivityType? type = e.ActivityType switch
            {
                DetectedActivity.InVehicle => MotionActivityType.Automotive,
                DetectedActivity.OnBicycle => MotionActivityType.Cycling,
                DetectedActivity.Running => MotionActivityType.Running,
                DetectedActivity.Walking => MotionActivityType.Walking,
                DetectedActivity.Still => MotionActivityType.Stationary,
                _ => null
            };
            if (type != null)
            {
                var trans = new MotionActivityTransition(type.Value, entry);
                await this.serviceProvider.RunDelegates<IAndroidActivityTransitionDelegate>(del => del.OnTransition(trans), this.logger);
                this.transitionSubj.OnNext(trans);
            }
        }
    }

    protected static ActivityTransition CreateTransition(MotionActivityType activityType, bool entry)
    {
        var native = ToNative(activityType);
        return new ActivityTransition.Builder()
            .SetActivityType(native)
            .SetActivityTransition(entry
                ? ActivityTransition.ActivityTransitionEnter
                : ActivityTransition.ActivityTransitionExit
            )
            .Build();
    }


    protected static int ToNative(MotionActivityType activityType) => activityType switch
    {
        MotionActivityType.Automotive => DetectedActivity.InVehicle,
        MotionActivityType.Cycling => DetectedActivity.OnBicycle,
        MotionActivityType.Running => DetectedActivity.Running,
        MotionActivityType.Walking => DetectedActivity.Walking,
        MotionActivityType.Stationary => DetectedActivity.Still,
        _ => throw new ArgumentException("Invalid MotionActivityType")
    };
}
