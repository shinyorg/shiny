using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Location;


namespace Shiny.Locations
{
    public class MotionActivityImpl : IMotionActivity
    {
        readonly AndroidContext context;
        readonly ActivityRecognitionClient client;
        readonly IMessageBus messageBus;


        public MotionActivityImpl(AndroidContext context, IMessageBus messageBus)
        {
            this.context = context;
            this.messageBus = messageBus;
            this.client = ActivityRecognition.GetClient(context.AppContext);
        }


        public bool IsSupported => true;


        public async Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset end)
        {
            await this.client.RequestActivityUpdatesAsync(5000, this.GetPendingIntent());
            //await this.client.RemoveActivityUpdatesAsync(this.GetPendingIntent());
            return null;
        }


        public IObservable<MotionActivityEvent> WhenActivityChanged() => this.messageBus.Listener<MotionActivityEvent>();


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
