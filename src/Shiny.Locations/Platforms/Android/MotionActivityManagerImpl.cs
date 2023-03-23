using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Location;
using Microsoft.Extensions.Logging;
using static Android.Manifest;

namespace Shiny.Locations;


public class MotionActivityManagerImpl : NotifyPropertyChanged, IMotionActivityManager, IShinyStartupTask
{
    public static TimeSpan TimeSpanBetweenUpdates { get; set; } = TimeSpan.FromSeconds(10);
    public static TimeSpan OldEventPurgeTime { get; set; } = TimeSpan.FromDays(60);

    public const string IntentAction = ReceiverName + ".INTENT_ACTION";
    public const string ReceiverName = "com.shiny.locations." + nameof(MotionActivityBroadcastReceiver);
    public static int HighConfidenceValue { get; set; } = 70;
    public static int MediumConfidenceValue { get; set; } = 40;

    readonly Subject<MotionActivityEvent> eventSubj = new();
    readonly ActivityRecognitionClient client;
    readonly AndroidSqliteDatabase database;
    readonly AndroidPlatform platform;
    readonly ILogger logger;
    PendingIntent? pendingIntent;


    public MotionActivityManagerImpl(
        AndroidPlatform platform,
        AndroidSqliteDatabase database,
        ILogger<IMotionActivityManager> logger
    )
    {
        this.platform = platform;
        this.database = database;
        this.logger = logger;
        this.client = ActivityRecognition.GetClient(platform.AppContext);
    }


    bool started;
    public bool IsStarted
    {
        get => this.started;
        set => this.Set(ref this.started, value);
    }


    public async void Start()
    {
        this.platform.RegisterBroadcastReceiver<MotionActivityBroadcastReceiver>(
            MotionActivityManagerImpl.IntentAction,
            Intent.ActionBootCompleted
        );
        MotionActivityBroadcastReceiver.Process = async result =>
        {
            var type = MotionActivityType.Unknown;

            foreach (var activity in result.ProbableActivities)
            {
                switch (activity.Type)
                {
                    case DetectedActivity.InVehicle:
                        type |= MotionActivityType.Automotive;
                        break;

                    case DetectedActivity.OnBicycle:
                        type |= MotionActivityType.Cycling;
                        break;

                    case DetectedActivity.OnFoot:
                    case DetectedActivity.Walking:
                        type |= MotionActivityType.Walking;
                        break;

                    case DetectedActivity.Running:
                        type |= MotionActivityType.Running;
                        break;

                    case DetectedActivity.Still:
                        type |= MotionActivityType.Stationary;
                        break;
                }
            }
            var confidence = ToConfidence(result.MostProbableActivity.Confidence);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            await this.database
                .ExecuteNonQuery(
                    $"INSERT INTO motion_activity(Event, Confidence, Timestamp) VALUES ({(int)type}, {(int)confidence}, {timestamp})"
                )
                .ConfigureAwait(false);
        };
        if (this.IsStarted)
        {
            try
            {
                // TODO
                //this.client.RequestActivityTransitionUpdates()
                await this.client
                    .RequestActivityUpdatesAsync(
                        Convert.ToInt32(TimeSpanBetweenUpdates.TotalMilliseconds),
                        this.GetPendingIntent()
                    )
                    .ConfigureAwait(false);

                var last = DateTimeOffset.UtcNow.Subtract(OldEventPurgeTime);
                await this.database.ExecuteNonQuery("DELETE FROM motion_activity WHERE Timestamp < " + last.Ticks);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error restarting motion activity logging");
            }
        }
    }


    public async Task<AccessState> RequestAccess()
    {
        var result = AccessState.Available;

        if (OperatingSystemShim.IsAndroidVersionAtLeast(29))
        {
            result = await this.platform
                .RequestAccess(Permission.ActivityRecognition)
                .ToTask()
                .ConfigureAwait(false);

            if (result == AccessState.Available && !this.IsStarted)
            {
                await this.client
                    .RequestActivityUpdatesAsync(
                        Convert.ToInt32(TimeSpanBetweenUpdates.TotalMilliseconds),
                        this.GetPendingIntent()
                    )
                    .ConfigureAwait(false);

                this.IsStarted = true;
            }
        }
        else if (!this.IsStarted)
        {
            await this.client
                .RequestActivityUpdatesAsync(
                    Convert.ToInt32(TimeSpanBetweenUpdates.TotalMilliseconds),
                    this.GetPendingIntent()
                )
                .ConfigureAwait(false);

            this.IsStarted = true;
        }
        return result;
    }


    public async Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null)
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();

        var st = start.ToUnixTimeSeconds();
        var et = (end ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        var sql = $@"SELECT 
    Confidence, 
    Event, 
    Timestamp 
FROM 
    motion_activity 
WHERE
    Timestamp > {st} AND Timestamp < {et}
ORDER BY 
    Timestamp DESC";

        return await this.database
            .RawQuery(
                sql,
                cursor =>
                {
                    var confidence = cursor.GetInt(0);
                    var events = cursor.GetInt(1);
                    var epochSeconds = cursor.GetLong(2);
                    var dt = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);

                    return new MotionActivityEvent(
                        (MotionActivityType)events,
                        (MotionActivityConfidence)confidence,
                        dt
                    );
                }
            )
            .ConfigureAwait(false);
    }

    public IObservable<MotionActivityEvent> WhenActivityChanged()
        => this.eventSubj;


    protected virtual PendingIntent GetPendingIntent()
        => this.pendingIntent ??= this.platform.GetBroadcastPendingIntent<MotionActivityBroadcastReceiver>(IntentAction, PendingIntentFlags.UpdateCurrent);


    static MotionActivityConfidence ToConfidence(int value)
    {
        if (value >= MotionActivityManagerImpl.HighConfidenceValue)
            return MotionActivityConfidence.High;

        if (value >= MotionActivityManagerImpl.MediumConfidenceValue)
            return MotionActivityConfidence.Medium;

        return MotionActivityConfidence.Low;
    }
}
