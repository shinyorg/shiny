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
        public const string ReceiverName = "org.shiny.push." + nameof(ShinyPushNotificationBroadcastReceiver);
        internal static Func<Intent, Task>? ProcessIntent { get; set; }


        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            if (ProcessIntent != null && intent != null)
                await ProcessIntent.Invoke(intent).ConfigureAwait(false);
        }
    }
}
