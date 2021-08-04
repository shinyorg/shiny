using System;

using Shiny.Infrastructure;
using Shiny.Notifications;


namespace Shiny.Push.Platforms.Android
{
    public class AndroidPushNotificationManager : AndroidNotificationManager
    {
        public AndroidPushNotificationManager(ShinyCoreServices core) : base(core)
        {
        }

        //static int counter = 100;
        //protected virtual PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
        //{
        //    var intent = this.core.Android.CreateIntent<ShinyNotificationBroadcastReceiver>(ShinyNotificationBroadcastReceiver.EntryIntentAction);
        //    var content = this.core.Serializer.Serialize(notification);
        //    intent
        //        .PutExtra(AndroidNotificationProcessor.IntentNotificationKey, content)
        //        .PutExtra(AndroidNotificationProcessor.IntentActionKey, action.Identifier);

        //    counter++;
        //    var pendingIntent = PendingIntent.GetBroadcast(
        //        this.core.Android.AppContext,
        //        counter,
        //        intent,
        //        PendingIntentFlags.UpdateCurrent
        //    )!;
        //    return pendingIntent;
        //}
    }
}
