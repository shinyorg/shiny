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
    public class ShinyPushNotificationBroadcastReceiver : ShinyBroadcastReceiver
    {
        public const string ReceiverName = "com.shiny.push." + nameof(ShinyNotificationBroadcastReceiver);
        public const string EntryIntentAction = ReceiverName + ".ENTRY_ACTION";


        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            if (intent.Action == EntryIntentAction)
            {
                //await this.Resolve<AndroidNotificationProcessor>().TryProcessIntent(intent);
                context?.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));
            }
        }
    }
}
