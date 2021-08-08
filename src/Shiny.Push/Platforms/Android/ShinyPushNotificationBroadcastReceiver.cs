using System;
using System.Threading.Tasks;
using Android.Content;
using Shiny.Push.Infrastructure;


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
        internal static Func<Intent, Task>? ProcessIntent { get; set; }


        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            if (intent?.Action == EntryIntentAction && ProcessIntent != null)
            {
                await ProcessIntent.Invoke(intent).ConfigureAwait(false);
                context?.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));
            }
        }
    }
}
