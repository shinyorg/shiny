using System;

using Microsoft.Extensions.Logging;


namespace Shiny.Notifications.Logging
{
    public class NotificationLogger : ILogger
    {
        readonly string categoryName;
        readonly INotificationManager notificationManager;


        public NotificationLogger(string categoryName, INotificationManager notificationManager)
        {
            this.categoryName = categoryName;
            this.notificationManager = notificationManager;
        }


        public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);

            await this.notificationManager.Send(new Notification
            {
                Title = $"[{logLevel}] {this.categoryName}",
                Message = message
            });
        }
    }
}
