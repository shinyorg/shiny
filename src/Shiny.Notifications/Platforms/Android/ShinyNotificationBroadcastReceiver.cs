using System;
using System.Threading.Tasks;
using Android.Content;


namespace Shiny.Notifications
{
    [BroadcastReceiver(
        Name = ReceiverName,
        Enabled = true,
        Exported = false
    )]
    //[IntentFilter(new[] {
    //    EntryIntentAction,
    //    AlarmIntentAction
    //})]
    public class ShinyNotificationBroadcastReceiver : ShinyBroadcastReceiver
    {
        public const string ReceiverName = "com.shiny.notifications." + nameof(ShinyNotificationBroadcastReceiver);
        public const string EntryIntentAction = ReceiverName + ".ENTRY_ACTION";
        //public const string AlarmIntentAction = ReceiverName + ".ALARM_ACTION";


        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            switch (intent.Action)
            {
                //case AlarmIntentAction:
                //    var notificationId = intent.GetIntExtra("NotificationId", 0);
                //    if (notificationId > 0)
                //    {
                //        var repo = this.Resolve<IRepository>();
                //        var notification = await repo.Get<Notification>(notificationId.ToString());
                //        notification.ScheduleDate = null;
                //        await this.Resolve<INotificationManager>().Send(notification);
                //        await repo.Remove<Notification>(notificationId.ToString());
                //    }
                //    break;

                case EntryIntentAction:
                    await this.Resolve<AndroidNotificationProcessor>().TryProcessIntent(intent);
                    context?.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));
                    break;
            }
        }
    }
}
