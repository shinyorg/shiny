using System;
using Android.App;
using Android.Content;
using Shiny.Infrastructure;
using RemoteInput = Android.Support.V4.App.RemoteInput;


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
    public class NotificationBroadcastReceiver : BroadcastReceiver
    {
        public const string ReceiverName = "com.shiny.locations." + nameof(NotificationBroadcastReceiver);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";


        public override void OnReceive(Context context, Intent intent)
        {
            var bundle = RemoteInput.GetResultsFromIntent(intent);
            if (bundle != null)
            {
                var text = bundle.GetString("Result");
                var notificationId = bundle.GetString("NotificationId", String.Empty);

                this.Execute(async () =>
                {
                    var notification = await ShinyHost.Resolve<IRepository>().Get<Notification>(notificationId);
                    if (notification != null)
                    {
                        ShinyHost
                            .Resolve<INotificationDelegate>()?
                            .OnEntry(new NotificationResponse(notification, null, text));
                    }
                });
                //int messageId = intent.getIntExtra(KEY_MESSAGE_ID, 0);
            }
        }
    }
}
