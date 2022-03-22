using System.Threading.Tasks;
using Android.App;
using Android.Content;

using Shiny.Infrastructure;

namespace Shiny.Notifications
{
    [BroadcastReceiver(
        Name = ReceiverName,
        Enabled = true,
        Exported = false
    )]
    [IntentFilter(
        new[] { Intent.ActionBootCompleted }
    )]
    public class ShinyNotificationBroadcastReceiver : ShinyBroadcastReceiver
    {
        public const string ReceiverName = "com.shiny.notifications." + nameof(ShinyNotificationBroadcastReceiver);
        public const string EntryIntentAction = ReceiverName + ".ENTRY_ACTION";
        public const string AlarmIntentAction = ReceiverName + ".ALARM_ACTION";


        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            switch (intent?.Action)
            {
                case Intent.ActionBootCompleted:
                    await this.ProcessPending();
                    break;

                case AlarmIntentAction:
                    await this.ProcessAlarm(intent);
                    break;

                case EntryIntentAction:
                    await this.Resolve<AndroidNotificationProcessor>().TryProcessIntent(intent);
                    context?.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));
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
                    break;
            }
        }


        async Task ProcessPending()
        {
            // TODO: fire anything pending that missed alarms?
            // TODO: if repeating, set next time
        }


        async Task ProcessAlarm(Intent intent)
        {
            var repo = this.Resolve<IRepository>();

            // TODO: nullify scheduledate to send now
            // TODO: get notification for alarm
            // TODO: if repeating, set next time
        }
    }
}
