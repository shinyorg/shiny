using System;
using UserNotifications;


namespace Shiny.Notifications
{
    public class ResponseArgs
    {
        public ResponseArgs(UNNotificationResponse response, Action completionHandler)
        {
            this.Response = response;
            this.CompletionHandler = completionHandler;
        }

        public UNNotificationResponse Response { get; }
        public Action CompletionHandler { get; }
    }
}
