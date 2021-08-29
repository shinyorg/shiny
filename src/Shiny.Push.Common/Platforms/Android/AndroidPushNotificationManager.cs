using System;
using Android.App;
using Android.Content;
using Firebase.Messaging;
using Shiny.Infrastructure;
using Shiny.Notifications;
using Notification = Shiny.Notifications.Notification;


namespace Shiny.Push
{
    public class AndroidPushNotificationManager : AndroidNotificationManager
    {
        const string INTENT_KEY = "ShinyAndroidPush";


        public AndroidPushNotificationManager(ShinyCoreServices services) : base(services) {}

        static int counter = 10000;
        protected override PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
        {
            var intent = this.Services.Android.CreateIntent<ShinyPushNotificationBroadcastReceiver>(ShinyPushNotificationBroadcastReceiver.EntryIntentAction);
            this.PopulateIntent(intent, notification);
            intent.PutExtra(INTENT_KEY, action.Identifier);

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
            intent.PutExtra(INTENT_KEY, content);
        }


        public PushNotificationResponse? FromIntent(Intent intent)
        {
            if (!intent.HasExtra(INTENT_KEY))
                return null;

            var notificationString = intent.GetStringExtra(INTENT_KEY);
            var notification = this.Services.Serializer.Deserialize<Shiny.Notifications.Notification>(notificationString);

            var action = intent.GetStringExtra(ShinyPushNotificationBroadcastReceiver.EntryIntentAction);
            var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");
            var response = new PushNotificationResponse(notification, action, text);

            return response;
        }


        public static PushNotification FromNative(RemoteMessage message)
        {
            Notification? notification = null;
            var native = message.GetNotification();

            if (native != null)
            {
                notification = new Notification
                {
                    Title = native.Title,
                    Message = native.Body,
                    Channel = native.ChannelId
                };
                if (!native.Icon.IsEmpty())
                    notification.Android.SmallIconResourceName = native.Icon;

                if (!native.Color.IsEmpty())
                    notification.Android.ColorResourceName = native.Color;
            }
            return new PushNotification(message.Data, notification);
        }
    }
}
