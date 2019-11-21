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
                var actionId = intent.GetStringExtra("ActionId");
                var notificationId = intent.GetIntExtra("NotificationId", 0);
                
                this.Execute(async () =>
                {
                    var notification = await ShinyHost
                        .Resolve<IRepository>()
                        .Get<Notification>(notificationId.ToString());

                    if (notification != null)
                    {
                        ShinyHost
                            .Resolve<INotificationDelegate>()?
                            .OnEntry(new NotificationResponse(notification, actionId, text));
                    }
                });
            }
        }
    }
}
