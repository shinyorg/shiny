using System;

using Shiny.Infrastructure;
using Shiny.Notifications;


namespace Shiny.Push
{
    public class AndroidPushNotificationManager : AndroidNotificationManager
    {
        public AndroidPushNotificationManager(ShinyCoreServices core) : base(core)
        {
        }

        //// TODO: Broadcast Receiver Generic Type, entry intent action, and intent key all need to be changed for push
        //// TODO: how does user customize
        //// TODO: override for push - set new broadcast receiver
        //static int counter = 100;
        //protected virtual PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
        //{
        //    var intent = this.services.Android.CreateIntent<ShinyNotificationBroadcastReceiver>(ShinyNotificationBroadcastReceiver.EntryIntentAction);
        //    this.PopulateIntent(intent, notification);
        //    intent.PutExtra(AndroidNotificationProcessor.IntentActionKey, action.Identifier);

        //    counter++;
        //    var pendingIntent = PendingIntent.GetBroadcast(
        //        this.services.Android.AppContext,
        //        counter,
        //        intent,
        //        PendingIntentFlags.UpdateCurrent
        //    )!;
        //    return pendingIntent;
        //}


        //// TODO: override for push - set intent notification key for broadcast receiver & onnewintent
        //protected virtual void PopulateIntent(Intent intent, Notification notification)
        //{
        //    var content = this.services.Serializer.Serialize(notification);
        //    intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, content);
        //}
    }
}
