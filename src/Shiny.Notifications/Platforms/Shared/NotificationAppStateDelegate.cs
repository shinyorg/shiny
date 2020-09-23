using System;


namespace Shiny.Notifications
{
    public class NotificationAppStateDelegate : ShinyAppStateDelegate
    {
        readonly INotificationManager manager;
        readonly Notification notification;


        public NotificationAppStateDelegate(INotificationManager manager, Notification notification)
        {
            this.manager = manager; 
            this.notification = notification;
        }


        public override async void OnStop()
        {
            base.OnStop();

            // TODO: this won't be waited - working on it with appstate manager
            await this.manager.Send(this.notification);
        }
    }
}
