using System;


namespace Shiny.Notifications
{
    class AppTerminateNotificationAppStateDelegate : ShinyAppStateDelegate
    {
        public static string? Title { get; set; }
        public static string? Message { get; set; }
        readonly INotificationManager notifications;


        public AppTerminateNotificationAppStateDelegate(INotificationManager notifications)
        {
            this.notifications = notifications;
        }


        public override void OnTerminate()
        {
            this.notifications.Send(new Notification
            {
                Title = Title,
                Message = Message
            });
        }
    }
}
