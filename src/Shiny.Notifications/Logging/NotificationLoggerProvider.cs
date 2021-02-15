using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Notifications.Logging
{
    public class NotificationLoggerProvider : ILoggerProvider
    {
        readonly INotificationManager notificationManager;
        public NotificationLoggerProvider(INotificationManager notificationManager)
            => this.notificationManager = notificationManager;

        public ILogger CreateLogger(string categoryName) => new NotificationLogger(categoryName, this.notificationManager);
        public void Dispose() { }
    }
}