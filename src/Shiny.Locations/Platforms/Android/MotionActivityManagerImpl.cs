using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Location;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;

using static Android.Manifest;


namespace Shiny.Locations
{
    public class MotionActivityManagerImpl : NotifyPropertyChanged, IMotionActivityManager, IShinyStartupTask
    {
        public static TimeSpan TimeSpanBetweenUpdates { get; set; } = TimeSpan.FromSeconds(10);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";
        public const string ReceiverName = "com.shiny.locations." + nameof(MotionActivityBroadcastReceiver);
        public static int HighConfidenceValue { get; set; } = 70;
        public static int MediumConfidenceValue { get; set; } = 40;

        readonly Subject<MotionActivityEvent> eventSubj;
        readonly ActivityRecognitionClient client;
        readonly AndroidSqliteDatabase database;
        readonly ShinyCoreServices core;
        readonly ILogger logger;
        PendingIntent? pendingIntent;


        public MotionActivityManagerImpl(ShinyCoreServices core,
                                         AndroidSqliteDatabase database,
                                         ILogger<IMotionActivityManager> logger)
        {
            this.core = core;
            this.database = database;
            this.logger = logger;
            this.client = ActivityRecognition.GetClient(core.Android.AppContext);
            this.eventSubj = new Subject<MotionActivityEvent>();
        }


        bool started;
        public bool IsStarted
        {
            get => this.started;
            set => this.Set(ref this.started, value);
        }


        public async void Start()
        {
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
                // DELETE FROM motion_activity WHERE Timestamp < DateTimeOffset.UtcNow.AddDays(-30).Ticks
                await this.database.ExecuteNonQuery(
                    $"INSERT INTO motion_activity(Event, Confidence, Timestamp) VALUES ({(int)type}, {(int)confidence}, {timestamp})"
                );
            };
            if (this.IsStarted)
            {
                try
                {
                    await this.client.RequestActivityUpdatesAsync(
                        Convert.ToInt32(TimeSpanBetweenUpdates.TotalMilliseconds),
                        this.GetPendingIntent()
                    );
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

            if (this.core.Android.IsMinApiLevel(29))
            {
                result = await this.core
                    .Android
                    .RequestAccess(Permission.ActivityRecognition)
                    .ToTask();

                if (result == AccessState.Available)
                {
                    await this.client.RequestActivityUpdatesAsync(
                        Convert.ToInt32(TimeSpanBetweenUpdates.TotalMilliseconds),
                        this.GetPendingIntent()
                    );
                }
            }
            this.IsStarted = result == AccessState.Available;

            return result;
        }


        public async Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null)
        {
            (await this.RequestAccess()).Assert();

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

            return await this.database.RawQuery(
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
            );
        }

        public IObservable<MotionActivityEvent> WhenActivityChanged()
            => this.eventSubj;


        protected virtual PendingIntent GetPendingIntent()
        {
            if (this.pendingIntent != null)
                return this.pendingIntent;

            var intent = this.core.Android.CreateIntent<MotionActivityBroadcastReceiver>(IntentAction);
            this.pendingIntent = PendingIntent.GetBroadcast(
                this.core.Android.AppContext,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent
            );
            return this.pendingIntent;
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
}
