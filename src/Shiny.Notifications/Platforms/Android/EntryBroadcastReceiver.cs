using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Shiny.Infrastructure;
using RemoteInput = AndroidX.Core.App.RemoteInput;


namespace Shiny.Notifications
{
    [BroadcastReceiver(
        Name = ReceiverName,
        Enabled = true,
        Exported = false
    )]
    [IntentFilter(new[] {
        IntentAction
    })]
    public class EntryBroadcastReceiver : ShinyBroadcastReceiver
    {
        public const string ReceiverName = "com.shiny.notifications." + nameof(EntryBroadcastReceiver);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";


        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            await this.Resolve<AndroidNotificationProcessor>().TryProcessIntent(intent);
            context.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));
        }
    }
}
