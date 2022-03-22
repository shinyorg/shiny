using System;
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
                    await this.Resolve<AndroidNotificationProcessor>().ProcessPending();
                    break;

                case AlarmIntentAction:
                    await this.Resolve<AndroidNotificationProcessor>().ProcessAlarm(intent);
                    break;

                case EntryIntentAction:
                    await this.Resolve<AndroidNotificationProcessor>().TryProcessIntent(intent);
                    context?.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));
                    break;
            }
        }
    }
}
