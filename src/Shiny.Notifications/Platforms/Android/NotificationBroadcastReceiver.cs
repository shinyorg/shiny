using System;
using Android.App;
using Android.Content;
using Shiny.Infrastructure;
#if ANDROIDX
using RemoteInput = AndroidX.Core.App.RemoteInput;
#else
using RemoteInput = Android.Support.V4.App.RemoteInput;
#endif


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
            var ndelegate = ShinyHost.Resolve<INotificationDelegate>();
            if (ndelegate == null)
                return;

            this.Execute(async () =>
            {
                var manager = ShinyHost.Resolve<INotificationManager>();
                var serializer = ShinyHost.Resolve<ISerializer>();

                var stringNotification = intent.GetStringExtra("Notification");
                var action = intent.GetStringExtra("Action");
                var notification = serializer.Deserialize<Notification>(stringNotification);
                var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");

                ShinyHost
                    .Resolve<AndroidContext>()
                    .AppContext
                    .SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));

                await ndelegate.OnEntry(new NotificationResponse(notification, action, text));
                await manager.Cancel(notification.Id);
            });
        }
    }
}
