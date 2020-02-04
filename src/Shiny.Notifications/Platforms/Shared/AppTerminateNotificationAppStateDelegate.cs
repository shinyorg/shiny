using System;


namespace Shiny.Notifications
{
    class AppTerminateNotificationAppStateDelegate : ShinyAppStateDelegate
    {
        public static Notification? Notification { get; set; }
        readonly INotificationManager notifications;


        public AppTerminateNotificationAppStateDelegate(INotificationManager notifications)
            => this.notifications = notifications;


        public override void OnTerminate()
            => this.notifications.Send(Notification).Wait();
    }
}
