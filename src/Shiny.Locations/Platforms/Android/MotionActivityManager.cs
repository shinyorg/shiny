using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Location;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


public class MotionActivityManager(
    AndroidPlatform platform,
    IServiceProvider services,
    ILogger<MotionActivityManager> logger
) : IMotionActivityManager, IShinyStartupTask
{
    public const string ReceiverName = "com.shiny.locations." + nameof(MotionActivityBroadcastReceiver);
    public const string TransitionIntentAction = ReceiverName + ".TRANSITION_ACTION";
    public const string UpdateIntentAction = ReceiverName + ".UPDATE_ACTION";

    PendingIntent? pendingTransitionIntent;
    PendingIntent? pendingUpdateIntent;


    bool isListening;
    public bool IsListening
    {
        get => this.isListening;
        private set => this.isListening = value;
    }


    public AccessState GetCurrentStatus()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
            return platform.GetCurrentPermissionStatus(Android.Manifest.Permission.ActivityRecognition);

        return AccessState.Available;
    }


    public async Task<AccessState> RequestAccess()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            return AccessState.Available;

        var result = await platform
            .RequestPermissions(Android.Manifest.Permission.ActivityRecognition);

        return result.IsGranted(Android.Manifest.Permission.ActivityRecognition)
            ? AccessState.Available
            : AccessState.Denied;
    }


    MotionActivityReading? lastReading;

    public Task<MotionActivityReading?> GetLastReading()
        => Task.FromResult(this.lastReading);


    public event EventHandler<MotionActivityReading>? MotionActivityReadingReceived;


    public Task StartListener() => StartListener(detailed: true);

    public async Task StartListener(bool detailed)
    {
        if (this.IsListening)
            throw new InvalidOperationException("Motion activity listener is already running");

        var client = ActivityRecognition.GetClient(platform.AppContext);
        if (detailed)
        {
            await client.RequestActivityUpdates(60000, this.GetPendingUpdateIntent()).ToTask();
        }
        else
        {
            var transitions = new List<ActivityTransition>
                {
                    BuildTransition(DetectedActivity.InVehicle, ActivityTransition.ActivityTransitionEnter),
                    BuildTransition(DetectedActivity.InVehicle, ActivityTransition.ActivityTransitionExit),
                    BuildTransition(DetectedActivity.OnBicycle, ActivityTransition.ActivityTransitionEnter),
                    BuildTransition(DetectedActivity.OnBicycle, ActivityTransition.ActivityTransitionExit),
                    BuildTransition(DetectedActivity.OnFoot, ActivityTransition.ActivityTransitionEnter),
                    BuildTransition(DetectedActivity.OnFoot, ActivityTransition.ActivityTransitionExit),
                    BuildTransition(DetectedActivity.Running, ActivityTransition.ActivityTransitionEnter),
                    BuildTransition(DetectedActivity.Running, ActivityTransition.ActivityTransitionExit),
                    BuildTransition(DetectedActivity.Still, ActivityTransition.ActivityTransitionEnter),
                    BuildTransition(DetectedActivity.Still, ActivityTransition.ActivityTransitionExit),
                    BuildTransition(DetectedActivity.Walking, ActivityTransition.ActivityTransitionEnter),
                    BuildTransition(DetectedActivity.Walking, ActivityTransition.ActivityTransitionExit),
                };

            var request = new ActivityTransitionRequest(transitions);
            await client.RequestActivityTransitionUpdates(request, this.GetPendingTransitionIntent()).ToTask();
        }

        this.IsListening = true;
    }
    

    public async Task StopListener()
    {
        if (!this.IsListening)
            return;

        var client = ActivityRecognition.GetClient(platform.AppContext);
        await client.RemoveActivityUpdates(this.GetPendingUpdateIntent()).ToTask();
        await client.RemoveActivityTransitionUpdates(this.GetPendingTransitionIntent()).ToTask();

        this.pendingTransitionIntent = null;
        this.IsListening = false;
    }


    public async void Start()
    {
        MotionActivityBroadcastReceiver.ProcessTransitions = async result =>
        {
            for (var i = 0; i < result.TransitionEvents.Count; i++)
            {
                var e = result.TransitionEvents[i];

                MotionActivityType activityType;
                if (e.TransitionType == ActivityTransition.ActivityTransitionExit)
                {
                    //If there is a next transition at the same time that is the start of a new state, we can skip this 'unknown' state
                    var nextTransitionIdx = i + 1;
                    if (nextTransitionIdx < result.TransitionEvents.Count && result.TransitionEvents[nextTransitionIdx].ElapsedRealTimeNanos == e.ElapsedRealTimeNanos)
                        continue;

                    activityType = MotionActivityType.Unknown;
                }
                else
                {
                    activityType = ToMotionActivityType(e.ActivityType);
                }

                var reading = new MotionActivityReading(
                    activityType,
                    MotionActivityConfidence.High,
                    GetTimeFromElapsedRealtimeNanos(e.ElapsedRealTimeNanos)
                );

                this.lastReading = reading;
                this.MotionActivityReadingReceived?.Invoke(this, reading);

                await services
                    .RunDelegates<IMotionActivityDelegate>(
                        x => x.OnReading(reading),
                        logger
                    )
                    .ConfigureAwait(false);
            }
        };

        MotionActivityBroadcastReceiver.ProcessUpdate = async result =>
        {
            var activityType = ToMotionActivityType((int)result.MostProbableActivity.Type);
            var confidence = ToMotionActivityConfidence(result.MostProbableActivity.Confidence);
            var reading = new MotionActivityReading(
                activityType,
                confidence,
                DateTimeOffset.UnixEpoch.AddMilliseconds(result.Time)
            );

            this.lastReading = reading;
            this.MotionActivityReadingReceived?.Invoke(this, reading);

            await services
                .RunDelegates<IMotionActivityDelegate>(
                    x => x.OnReading(reading),
                    logger
                )
                .ConfigureAwait(false);
        };

        if (!this.IsListening)
            return;

        try
        {
            this.isListening = false;
            await this.StartListener();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to auto-restart motion activity listener");
            this.IsListening = false;
        }
    }

    PendingIntent GetPendingTransitionIntent()
        => this.pendingTransitionIntent ??= platform.GetBroadcastPendingIntent<MotionActivityBroadcastReceiver>(
            TransitionIntentAction,
            PendingIntentFlags.UpdateCurrent
        );

    PendingIntent GetPendingUpdateIntent()
        => this.pendingUpdateIntent ??= platform.GetBroadcastPendingIntent<MotionActivityBroadcastReceiver>(
            UpdateIntentAction,
            PendingIntentFlags.UpdateCurrent
        );

    static ActivityTransition BuildTransition(int activityType, int transitionType)
        => new ActivityTransition.Builder()
            .SetActivityType(activityType)
            .SetActivityTransition(transitionType)
            .Build();

    static MotionActivityType ToMotionActivityType(int activityType) => activityType switch
    {
        DetectedActivity.InVehicle => MotionActivityType.Automotive,
        DetectedActivity.OnBicycle => MotionActivityType.Cycling,
        DetectedActivity.Running => MotionActivityType.Running,
        DetectedActivity.Walking => MotionActivityType.Walking,
        DetectedActivity.OnFoot => MotionActivityType.Walking,
        DetectedActivity.Still => MotionActivityType.Stationary,
        _ => MotionActivityType.Unknown
    };

    static MotionActivityConfidence ToMotionActivityConfidence(int confidence) => confidence switch
    {
        > 60 => MotionActivityConfidence.High,
        < 40 => MotionActivityConfidence.Low,
        _ => MotionActivityConfidence.Medium,
    };

    static DateTimeOffset GetTimeFromElapsedRealtimeNanos(long elapsedRealTimeNanos) =>
        DateTimeOffset.UtcNow.AddTicks((elapsedRealTimeNanos - Android.OS.SystemClock.ElapsedRealtimeNanos()) / TimeSpan.NanosecondsPerTick);
}
