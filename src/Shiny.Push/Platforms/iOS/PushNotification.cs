using System;
using UserNotifications;


namespace Shiny.Push
{
    class PushNotification : IPushNotification
    {
        readonly UNNotification native;
        public PushNotification(UNNotification native)
            => this.native = native;


        public string Title => this.native.Request.Content.Title;
        public string Body => this.native.Request.Content.Body;
    }
}
