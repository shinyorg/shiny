using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Location;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class MotionActivityImpl : IMotionActivity, IShinyStartupTask
    {
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
                // TODO: ping timer should be configurable?  10 seconds for now
                await this.client.RequestActivityUpdatesAsync(10000, this.GetPendingIntent());
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }


        public bool IsSupported => true;


        public Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset end)
            => this.database.RawQuery(
                $"SELECT Confidence, Event, Timestamp FROM motion_activity WHERE Timestamp BETWEEN ${start.Ticks} AND ${end.Ticks}",
                cursor =>
                {
                    var confidence = cursor.GetInt(0);
                    var events = cursor.GetInt(1);
                    var ticks = cursor.GetLong(2);
                    var dt = new DateTimeOffset(ticks, TimeSpan.FromHours(0));

                    return new MotionActivityEvent(
                        (MotionActivityType)events,
                        MotionActivityConfidence.Low,
                        dt
                    );
                }
            );


        public IObservable<MotionActivityEvent> WhenActivityChanged()
            => this.messageBus.Listener<MotionActivityEvent>();


        PendingIntent pendingIntent;
        protected virtual PendingIntent GetPendingIntent()
        {
            if (this.pendingIntent != null)
                return this.pendingIntent;

            var intent = new Intent(this.context.AppContext, typeof(MotionActivityBroadcastReceiver));
            //intent.SetAction(GeofenceBroadcastReceiver.INTENT_ACTION);
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
