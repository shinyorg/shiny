using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Gms.Location;
using Microsoft.Extensions.Logging;
using static Android.Manifest;

namespace Shiny.Locations;


public class MotionActivityManager : NotifyPropertyChanged, IMotionActivityManager, IShinyStartupTask
{
    public static TimeSpan TimeSpanBetweenUpdates { get; set; } = TimeSpan.FromMinutes(1);
    public static TimeSpan OldEventPurgeTime { get; set; } = TimeSpan.FromDays(60);
    public static bool UseMostProbableResult { get; set; }

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


    public MotionActivityManager(
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
        await this.PurgeOldEvents(OldEventPurgeTime);
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
        }
    }


    public Task<AccessState> RequestAccess()
    {
        var permissionKey = OperatingSystemShim.IsAndroidVersionAtLeast(29)
            ? Permission.ActivityRecognition
            : "com.google.android.gms.permission.ACTIVITY_RECOGNITION";

        return this.RequestAccessSpecific(permissionKey);
    }


    public async Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null)
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();

        var st = start.ToUnixTimeSeconds().ToString();
        var et = (end ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString();
        var sql = $$"""
            SELECT
                AutomotiveConfidence,
                CyclingConfidence,
                RunningConfidence,
                WalkingConfidence,
                StationaryConfidence,
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
                    var list = new List<DetectedMotionActivity>();
                    PopulateIf(list, cursor, 0, MotionActivityType.Automotive);
                    PopulateIf(list, cursor, 1, MotionActivityType.Cycling);
                    PopulateIf(list, cursor, 2, MotionActivityType.Running);
                    PopulateIf(list, cursor, 3, MotionActivityType.Walking);
                    PopulateIf(list, cursor, 4, MotionActivityType.Stationary);

                    var epochSeconds = cursor.GetLong(5);
                    var dt = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
                    var probable = list.OrderBy(x => x.Confidence).FirstOrDefault();
                    return new MotionActivityEvent(list, probable, dt);
                },
                st,
                et
            )
            .ConfigureAwait(false);
    }


    protected async Task<AccessState> RequestAccessSpecific(string permissionKey)
    {
        var result = await this.platform
            .RequestAccess(permissionKey)
            .ToTask()
            .ConfigureAwait(false);

        if (result == AccessState.Available && !this.IsStarted)
        {
            await this.StartActivityMonitor();
            this.IsStarted = true;
        }
        return result;
    }


    static void PopulateIf(List<DetectedMotionActivity> list, ICursor cursor, int columnIndex, MotionActivityType type)
    {
        if (cursor.IsNull(columnIndex))
            return;

        var conf = ToConfidence(cursor.GetInt(columnIndex));
        list.Add(new(type, conf));
    }


    public IObservable<MotionActivityEvent> WhenActivityChanged()
        => this.eventSubj;


    protected virtual PendingIntent GetPendingIntent()
        => this.pendingIntent ??= this.platform.GetBroadcastPendingIntent<MotionActivityBroadcastReceiver>(IntentAction, PendingIntentFlags.UpdateCurrent);


    public Task PurgeOldEvents(TimeSpan timeSpan)
    {
        var last = DateTimeOffset.UtcNow.Subtract(timeSpan);
        var sql = "DELETE FROM motion_activity WHERE Timestamp < " + last.ToUnixTimeSeconds();
        return this.database.ExecuteNonQuery(sql);
    }


    protected virtual async Task StartActivityMonitor()
    {
        this.platform.RegisterBroadcastReceiver<MotionActivityBroadcastReceiver>(
            MotionActivityManager.IntentAction,
            Intent.ActionBootCompleted
        );

        MotionActivityBroadcastReceiver.ProcessRecognition = async result =>
        {
            var list = new List<DetectedMotionActivity>();
            var auto = "NULL";
            var cycle = "NULL";
            var run = "NULL";
            var walk = "NULL";
            var still = "NULL";

            foreach (var activity in result.ProbableActivities)
            {
                var conf = ToConfidence(activity.Confidence);

                switch (activity.Type)
                {
                    case DetectedActivity.InVehicle:
                        auto = activity.Confidence.ToString();
                        list.Add(new(MotionActivityType.Automotive, conf));
                        break;

                    case DetectedActivity.OnBicycle:
                        cycle = activity.Confidence.ToString();
                        list.Add(new(MotionActivityType.Cycling, conf));
                        break;

                    case DetectedActivity.OnFoot:
                    case DetectedActivity.Walking:
                        walk = activity.Confidence.ToString();
                        list.Add(new(MotionActivityType.Walking, conf));
                        break;

                    case DetectedActivity.Running:
                        run = activity.Confidence.ToString();
                        list.Add(new(MotionActivityType.Running, conf));
                        break;

                    case DetectedActivity.Still:
                        still = activity.Confidence.ToString();
                        list.Add(new(MotionActivityType.Stationary, conf));
                        break;
                }
            }

            //result.ElapsedRealtimeMillis
            //result.Time
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await this.database
                .ExecuteNonQuery($"INSERT INTO motion_activity(AutomotiveConfidence, CyclingConfidence, RunningConfidence, WalkingConfidence, StationaryConfidence, Timestamp) VALUES ({auto}, {cycle}, {run}, {walk}, {still}, {timestamp})")
                .ConfigureAwait(false);

            DetectedMotionActivity? probable = null;
            if (result.MostProbableActivity.Type != DetectedActivity.Unknown)
            {
                probable = new DetectedMotionActivity(
                    result.MostProbableActivity.Type switch
                    {
                        DetectedActivity.InVehicle => MotionActivityType.Automotive,
                        DetectedActivity.OnBicycle => MotionActivityType.Cycling,
                        DetectedActivity.Running => MotionActivityType.Running,
                        DetectedActivity.Walking => MotionActivityType.Walking,
                        DetectedActivity.Tilting => MotionActivityType.Stationary,
                        DetectedActivity.Still => MotionActivityType.Stationary
                    },
                    ToConfidence(result.MostProbableActivity.Confidence)
                );
            }
            this.eventSubj.OnNext(new MotionActivityEvent(list, probable, DateTimeOffset.UtcNow));
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
        if (value >= MotionActivityManager.HighConfidenceValue)
            return MotionActivityConfidence.High;

        if (value >= MotionActivityManager.MediumConfidenceValue)
            return MotionActivityConfidence.Medium;

        return MotionActivityConfidence.Low;
    }
}
