using System;
using Shiny.Logging;


namespace Shiny.Notifications
{
    public class NotificationLogger : ILogger
    {
        public void Write(Exception exception, params (string Key, string Value)[] parameters)
            => ShinyHost.Resolve<INotificationManager>().Send("ERROR", exception.ToString());

        public void Write(string eventName, string description, params (string Key, string Value)[] parameters)
            => ShinyHost.Resolve<INotificationManager>().Send(eventName, description);
    }
}
