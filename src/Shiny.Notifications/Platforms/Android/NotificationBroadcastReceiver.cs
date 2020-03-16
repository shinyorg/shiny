using System;
using System.Linq;
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
        public const string ReceiverName = "com.shiny.notifications." + nameof(NotificationBroadcastReceiver);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";


        public override void OnReceive(Context context, Intent intent)
        {
            //if (intent.Action != IntentAction)
            //    return;

            var delegates = ShinyHost.ResolveAll<INotificationDelegate>();
            if (!delegates.Any())
                return;

            this.Execute(async () =>
            {
                var manager = ShinyHost.Resolve<INotificationManager>();
                var serializer = ShinyHost.Resolve<ISerializer>();

                var stringNotification = intent.GetStringExtra("Notification");
                var action = intent.GetStringExtra("Action");
                var notification = serializer.Deserialize<Notification>(stringNotification);
                var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");

                context.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));

                var response = new NotificationResponse(notification, action, text);
                await delegates.RunDelegates(x => x.OnEntry(response));
                await manager.Cancel(notification.Id);
            });
        }
    }
}
