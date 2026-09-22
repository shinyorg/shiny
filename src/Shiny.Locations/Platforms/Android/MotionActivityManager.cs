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
    public const string IntentAction = ReceiverName + ".INTENT_ACTION";

    PendingIntent? pendingIntent;


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


    public async Task StartListener()
    {
        if (this.IsListening)
            throw new InvalidOperationException("Motion activity listener is already running");

        var client = ActivityRecognition.GetClient(platform.AppContext);
        await client.RequestActivityUpdates(60000, this.GetPendingIntent()).ToTask();
        this.IsListening = true;
    }
    

    public async Task StopListener()
    {
        if (!this.IsListening)
            return;

        var client = ActivityRecognition.GetClient(platform.AppContext);
        await client.RemoveActivityUpdates(this.GetPendingUpdateIntent()).ToTask();

        this.pendingIntent = null;
        this.IsListening = false;
    }


    public async void Start()
    {
        MotionActivityBroadcastReceiver.Process = async result =>
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

    PendingIntent GetPendingIntent()
        => this.pendingIntent ??= platform.GetBroadcastPendingIntent<MotionActivityBroadcastReceiver>(
            IntentAction,
            PendingIntentFlags.UpdateCurrent
        );

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
}
