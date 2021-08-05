using System;
using System.Threading.Tasks;
using Android.Content;


namespace Shiny.Push
{
    [BroadcastReceiver(
        Name = ReceiverName,
        Enabled = true,
        Exported = false
    )]
    public class ShinyPushNotificationBroadcastReceiver : ShinyBroadcastReceiver
    {
        public const string ReceiverName = "com.shiny.push." + nameof(ShinyPushNotificationBroadcastReceiver);
        public const string EntryIntentAction = ReceiverName + ".ENTRY_ACTION";


        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            if (intent?.Action == EntryIntentAction)
            {
                await this.Resolve<AndroidPushProcessor>().TryProcessIntent(intent);
                context?.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));
            }
        }
    }
}
