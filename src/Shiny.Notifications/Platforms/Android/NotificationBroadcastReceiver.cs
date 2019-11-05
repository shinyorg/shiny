using System;
using Android.App;
using Android.Content;
using RemoteInput = Android.Support.V4.App.RemoteInput;


namespace Shiny.Notifications
{
    [BroadcastReceiver(
        Name = ReceiverName,
        Enabled = true,
        Exported = false
    )]
    [IntentFilter(new[] { IntentAction })]
    public class NotificationBroadcastReceiver : BroadcastReceiver
    {
        public const string ReceiverName = "com.shiny.locations." + nameof(NotificationBroadcastReceiver);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";


        public override void OnReceive(Context context, Intent intent)
        {
            var bundle = RemoteInput.GetResultsFromIntent(intent);
            if (bundle != null)
            {
                //bundle.GetString(KEY_REPLY);
                //int messageId = intent.getIntExtra(KEY_MESSAGE_ID, 0);
            }
        }
    }
}
