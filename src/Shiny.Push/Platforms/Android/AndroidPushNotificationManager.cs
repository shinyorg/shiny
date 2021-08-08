using System;
using Android.App;
using Android.Content;
using Shiny.Infrastructure;
using Shiny.Notifications;
using Notification = Shiny.Notifications.Notification;


namespace Shiny.Push
{
    public class AndroidPushNotificationManager : AndroidNotificationManager
    {
        public AndroidPushNotificationManager(ShinyCoreServices services) : base(services) {}

        static int counter = 10000;
        protected override PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
        {
            var intent = this.Services.Android.CreateIntent<ShinyPushNotificationBroadcastReceiver>(ShinyPushNotificationBroadcastReceiver.EntryIntentAction);
            this.PopulateIntent(intent, notification);
            //intent.PutExtra(AndroidPushProcessor.IntentActionKey, action.Identifier);

            counter++;
            var pendingIntent = PendingIntent.GetBroadcast(
                this.Services.Android.AppContext,
                counter,
                intent,
                PendingIntentFlags.UpdateCurrent
            )!;
            return pendingIntent;
        }


        protected override void PopulateIntent(Intent intent, Notification notification)
        {
            var content = this.Services.Serializer.Serialize(notification);
            //intent.PutExtra(AndroidPushProcessor.IntentNotificationKey, content);
        }
    }
}
