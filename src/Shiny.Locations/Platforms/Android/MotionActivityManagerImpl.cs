using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Location;
using Shiny.Logging;

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

        readonly ActivityRecognitionClient client;
        readonly IAndroidContext context;
        readonly AndroidSqliteDatabase database;
        readonly IMessageBus messageBus;
        PendingIntent? pendingIntent;


        public MotionActivityManagerImpl(IAndroidContext context,
                                         AndroidSqliteDatabase database,
                                         IMessageBus messageBus)
        {
            this.context = context;
            this.database = database;
            this.messageBus = messageBus;
            this.client = ActivityRecognition.GetClient(context.AppContext);
        }


        bool started;
        public bool IsStarted
        {
            get => this.started;
            set => this.Set(ref this.started, value);
        }


        public void Start()
        {
            if (this.IsStarted)
            {
                Log.SafeExecute(() =>
                    this.client.RequestActivityUpdatesAsync(
                        Convert.ToInt32(TimeSpanBetweenUpdates.TotalMilliseconds),
                        this.GetPendingIntent()
                    )
                );
            }
        }


        public async Task<AccessState> RequestAccess()
        {
            var result = AccessState.Available;

            if (this.context.IsMinApiLevel(29))
            {
                result = await this.context
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
            => this.messageBus.Listener<MotionActivityEvent>();


        protected virtual PendingIntent GetPendingIntent()
        {
            if (this.pendingIntent != null)
                return this.pendingIntent;

            var intent = this.context.CreateIntent<MotionActivityBroadcastReceiver>(IntentAction);
            this.pendingIntent = PendingIntent.GetBroadcast(
                this.context.AppContext,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent
            );
            return this.pendingIntent;
        }
    }
}
