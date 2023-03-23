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
    public static TimeSpan TimeSpanBetweenUpdates { get; set; } = TimeSpan.FromMinutes(1);
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


        if (this.IsStarted)
        {
            try
            {
                await this.StartActivityMonitor();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error restarting motion activity logging");
            }

            try
            {
                var last = DateTimeOffset.UtcNow.Subtract(OldEventPurgeTime);
                var sql = "DELETE FROM motion_activity WHERE Timestamp < " + last.ToUnixTimeSeconds();
                await this.database.ExecuteNonQuery(sql);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to purge old motion activity events");
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
                await this.StartActivityMonitor();
                this.IsStarted = true;
            }
        }
        else if (!this.IsStarted)
        {
            await this.StartActivityMonitor();
            this.IsStarted = true;
        }
        return result;
    }


    public async Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null)
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();

        var st = start.ToUnixTimeSeconds().ToString();
        var et = (end ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString();
        var sql = $$"""
            SELECT 
                Confidence, 
                Event, 
                Timestamp 
            FROM 
                motion_activity 
            WHERE
                Timestamp > ? AND Timestamp < ?
            ORDER BY 
                Timestamp DESC
            """;

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
                },
                st,
                et
            )
            .ConfigureAwait(false);
    }

    public IObservable<MotionActivityEvent> WhenActivityChanged()
        => this.eventSubj;


    protected virtual PendingIntent GetPendingIntent()
        => this.pendingIntent ??= this.platform.GetBroadcastPendingIntent<MotionActivityBroadcastReceiver>(IntentAction, PendingIntentFlags.UpdateCurrent);


    //protected virtual async Task StartActivityMonitor()
    //{

    //    MotionActivityBroadcastReceiver.ProcessTransition = async result =>
    //    {
    //        foreach (var e in result.TransitionEvents)
    //        {
    //            //e.ActivityType
    //        }
    //        //e.TransitionType == ActivityTransition.ActivityTransitionEnter // don't care
    //        //e.ActivityType = DetectedActivity.InVehicle
    //        //        await this.database
    //        //            .ExecuteNonQuery(
    //        //                $"INSERT INTO motion_activity(Event, Confidence, Timestamp) VALUES ({(int)type}, {(int)confidence}, {timestamp})"
    //        //            )
    //        //            .ConfigureAwait(false);
    //    };

    //    //e.ElapsedRealTimeNanos

    //    // we only care about enter events since we're tracking all types
    //    var list = new List<ActivityTransition>
    //    {
    //        GetTransition(DetectedActivity.InVehicle),
    //        GetTransition(DetectedActivity.OnBicycle),
    //        GetTransition(DetectedActivity.OnFoot),
    //        GetTransition(DetectedActivity.Running),
    //        GetTransition(DetectedActivity.Still),
    //        GetTransition(DetectedActivity.Tilting),
    //        GetTransition(DetectedActivity.Unknown),
    //        GetTransition(DetectedActivity.Walking)
    //    };

    //    // TODO: android task
    //    this.client.RequestActivityTransitionUpdates(
    //        new ActivityTransitionRequest(list),
    //        this.GetPendingIntent()
    //    );
    //}


    //protected static ActivityTransition GetTransition(int activityType)
    //    => new ActivityTransition.Builder()
    //        .SetActivityType(activityType)
    //        .SetActivityTransition(ActivityTransition.ActivityTransitionEnter)
    //        .Build();


    protected virtual async Task StartActivityMonitor()
    {
        MotionActivityBroadcastReceiver.ProcessRecognition = async result =>
        {
            var type = result.MostProbableActivity.Type switch
            {
                DetectedActivity.InVehicle => MotionActivityType.Automotive,
                DetectedActivity.OnBicycle => MotionActivityType.Cycling,
                DetectedActivity.OnFoot => MotionActivityType.Walking,
                DetectedActivity.Running => MotionActivityType.Running,
                DetectedActivity.Still => MotionActivityType.Stationary,
                _ => MotionActivityType.Unknown
            };

            //foreach (var activity in result.ProbableActivities)
            //{
            //    switch (activity.Type)
            //    {
            //        case DetectedActivity.InVehicle:
            //            type |= MotionActivityType.Automotive;
            //            break;

            //        case DetectedActivity.OnBicycle:
            //            type |= MotionActivityType.Cycling;
            //            break;

            //        case DetectedActivity.OnFoot:
            //        case DetectedActivity.Walking:
            //            type |= MotionActivityType.Walking;
            //            break;

            //        case DetectedActivity.Running:
            //            type |= MotionActivityType.Running;
            //            break;

            //        case DetectedActivity.Still:
            //            type |= MotionActivityType.Stationary;
            //            break;
            //    }
            //}
            //var confidence = ToConfidence(result.MostProbableActivity.Confidence);
            
            var confidence = ToConfidence(result.MostProbableActivity.Confidence);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await this.database
                .ExecuteNonQuery(
                    $"INSERT INTO motion_activity(Event, Confidence, Timestamp) VALUES ({(int)type}, {(int)confidence}, {timestamp})"
                )
                .ConfigureAwait(false);
        };

        await this.client
            .RequestActivityUpdatesAsync(
                Convert.ToInt32(TimeSpanBetweenUpdates.TotalMilliseconds),
                this.GetPendingIntent()
            )
            .ConfigureAwait(false);
    }


    static MotionActivityConfidence ToConfidence(int value)
    {
        if (value >= MotionActivityManagerImpl.HighConfidenceValue)
            return MotionActivityConfidence.High;

        if (value >= MotionActivityManagerImpl.MediumConfidenceValue)
            return MotionActivityConfidence.Medium;

        return MotionActivityConfidence.Low;
    }
}
