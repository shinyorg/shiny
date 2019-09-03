using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Location;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class MotionActivityImpl : IMotionActivity, IShinyStartupTask
    {
        public static TimeSpan TimeSpanBetweenUpdates { get; set; } = TimeSpan.FromSeconds(10);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";
        public const string ReceiverName = "com.shiny.locations." + nameof(MotionActivityBroadcastReceiver);
        public static int HighConfidenceValue { get; set; } = 70;
        public static int MediumConfidenceValue { get; set; } = 40;

        readonly ActivityRecognitionClient client;
        readonly AndroidContext context;
        readonly AndroidSqliteDatabase database;
        readonly IMessageBus messageBus;


        public MotionActivityImpl(AndroidContext context,
                                  AndroidSqliteDatabase database,
                                  IMessageBus messageBus)
        {
            this.context = context;
            this.database = database;
            this.messageBus = messageBus;
            this.client = ActivityRecognition.GetClient(context.AppContext);
        }


        public async void Start()
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
                Log.Write(ex);
            }
        }


        public bool IsSupported => true;


        public Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset end)
        {
            var st = start.ToUnixTimeSeconds();
            var et = end.ToUnixTimeSeconds();

//            $@"SELECT 
//    Confidence, 
//    Event, 
//    Timestamp 
//FROM 
//    motion_activity 
//WHERE 
//    Timestamp BETWEEN ${st} AND ${et} 
//ORDER BY 
//    Timestamp DESC",
            return this.database.RawQuery(
$@"SELECT 
    Confidence, 
    Event, 
    Timestamp 
FROM 
    motion_activity 
ORDER BY 
    Timestamp DESC",
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


        PendingIntent pendingIntent;
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
